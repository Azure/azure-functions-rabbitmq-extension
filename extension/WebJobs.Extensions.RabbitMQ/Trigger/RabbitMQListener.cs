// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal sealed class RabbitMQListener : IListener, IScaleMonitor<RabbitMQTriggerMetrics>
{
    private const string RequeueCountHeaderName = "x-ms-rabbitmq-requeuecount";

    private static readonly ActivitySource ActivitySource = new("Microsoft.Azure.WebJobs.Extensions.RabbitMQ");

    private readonly ITriggeredFunctionExecutor executor;
    private readonly string queueName;
    private readonly ushort prefetchCount;
    private readonly IRabbitMQService service;
    private readonly ILogger logger;
    private readonly string functionId;
    private readonly IRabbitMQModel rabbitMQModel;

    private EventingBasicConsumer consumer;
    private string consumerTag;
    private bool disposed;
    private bool started;

    public RabbitMQListener(
        ITriggeredFunctionExecutor executor,
        IRabbitMQService service,
        string queueName,
        ILogger logger,
        FunctionDescriptor functionDescriptor,
        ushort prefetchCount)
    {
        this.executor = executor;
        this.service = service;
        this.queueName = queueName;
        this.logger = logger;
        this.rabbitMQModel = this.service.RabbitMQModel;
        _ = functionDescriptor ?? throw new ArgumentNullException(nameof(functionDescriptor));
        this.functionId = functionDescriptor.Id;
        this.Descriptor = new ScaleMonitorDescriptor($"{this.functionId}-RabbitMQTrigger-{this.queueName}".ToLowerInvariant());
        this.prefetchCount = prefetchCount;
    }

    public ScaleMonitorDescriptor Descriptor { get; }

    public void Cancel()
    {
        if (!this.started)
        {
            return;
        }

        this.StopAsync(CancellationToken.None).Wait();
    }

    public void Dispose()
    {
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.ThrowIfDisposed();

        if (this.started)
        {
            throw new InvalidOperationException("The listener has already been started.");
        }

        this.rabbitMQModel.BasicQos(0, this.prefetchCount, false);  // Non zero prefetchSize doesn't work (tested upto 5.2.0) and will throw NOT_IMPLEMENTED exception
        this.consumer = new EventingBasicConsumer(this.rabbitMQModel.Model);

        this.consumer.Received += async (model, args) =>
        {
            // The RabbitMQ client rents an array from the ArrayPool to hold a copy the message body, and passes it to
            // the listener. Once all event handlers are executed, the array is returned back to the pool so that the
            // memory can be reused for future messages for that connection. However, since our event handler is async,
            // the very first await statement i.e. the call to TryExecuteAsync ends the event handler invocation and
            // let the RabbitMQ client reclaim the memory. This led to issue with message corruption on republish (see:
            // https://github.com/Azure/azure-functions-rabbitmq-extension/issues/211).
            //
            // Since the same args argument is passed to all event handlers, replacing it with a local copy (with
            // message body copied) will ensure that the other event handlers (in case they are present) will receive
            // exactly the same args as it was composed by the RabbitMQ client.
            args = new BasicDeliverEventArgs(args.ConsumerTag, args.DeliveryTag, args.Redelivered, args.Exchange, args.RoutingKey, args.BasicProperties, args.Body.ToArray());

            using Activity activity = StartActivity(args);

            var input = new TriggeredFunctionData() { TriggerValue = args };
            FunctionResult result = await this.executor.TryExecuteAsync(input, cancellationToken).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                args.BasicProperties.Headers ??= new Dictionary<string, object>();
                args.BasicProperties.Headers.TryGetValue(RequeueCountHeaderName, out object headerValue);
                int requeueCount = Convert.ToInt32(headerValue, CultureInfo.InvariantCulture) + 1;

                if (requeueCount >= 5)
                {
                    // Add message to dead letter exchange.
                    this.logger.LogDebug("Requeue count exceeded: rejecting message");
                    this.rabbitMQModel.BasicReject(args.DeliveryTag, false);
                    return;
                }

                this.logger.LogDebug("Republishing message");
                args.BasicProperties.Headers[RequeueCountHeaderName] = requeueCount;
                this.rabbitMQModel.BasicPublish(exchange: string.Empty, routingKey: this.queueName, args.BasicProperties, args.Body);
            }

            this.rabbitMQModel.BasicAck(args.DeliveryTag, false);
        };

        this.consumerTag = this.rabbitMQModel.BasicConsume(queue: this.queueName, autoAck: false, consumer: this.consumer);
        this.started = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.ThrowIfDisposed();

        if (!this.started)
        {
            throw new InvalidOperationException("The listener has not yet been started or has already been stopped");
        }

        this.rabbitMQModel.BasicCancel(this.consumerTag);
        this.rabbitMQModel.Close();
        this.started = false;
        this.disposed = true;
        return Task.CompletedTask;
    }

    async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync()
    {
        return await this.GetMetricsAsync().ConfigureAwait(false);
    }

    public Task<RabbitMQTriggerMetrics> GetMetricsAsync()
    {
        QueueDeclareOk queueInfo = this.rabbitMQModel.QueueDeclarePassive(this.queueName);
        var metrics = new RabbitMQTriggerMetrics
        {
            QueueLength = queueInfo.MessageCount,
            Timestamp = DateTime.UtcNow,
        };

        return Task.FromResult(metrics);
    }

    ScaleStatus IScaleMonitor.GetScaleStatus(ScaleStatusContext context)
    {
        return this.GetScaleStatusCore(context.WorkerCount, context.Metrics?.Cast<RabbitMQTriggerMetrics>().ToArray());
    }

    public ScaleStatus GetScaleStatus(ScaleStatusContext<RabbitMQTriggerMetrics> context)
    {
        return this.GetScaleStatusCore(context.WorkerCount, context.Metrics?.ToArray());
    }

    internal static Activity StartActivity(BasicDeliverEventArgs ea)
    {
        // Ideally, we would have used string-values for headers, but RabbitMQ client has an old quirk where it does
        // not differentiate between string headers and byte-array headers when decoding them. See:
        // https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/415. Hence, it is decided to also set byte[] as
        // value for 'traceparent' header for consistency between the below two cases.
        if (ea.BasicProperties.Headers?.ContainsKey("traceparent") == true)
        {
            byte[] traceParentIdInBytes = ea.BasicProperties.Headers["traceparent"] as byte[];
            string traceparentId = Encoding.UTF8.GetString(traceParentIdInBytes);
            return ActivitySource.StartActivity("Trigger", ActivityKind.Consumer, traceparentId);
        }
        else
        {
            Activity activity = ActivitySource.StartActivity("Trigger", ActivityKind.Consumer);

            // Method 'StartActivity' will return null if it has no event listeners.
            if (activity != null)
            {
                ea.BasicProperties.Headers ??= new Dictionary<string, object>();
                byte[] traceParentIdInBytes = Encoding.UTF8.GetBytes(activity.Id);
                ea.BasicProperties.Headers["traceparent"] = traceParentIdInBytes;
            }

            return activity;
        }
    }

    private static bool IsTrueForLast(IList<RabbitMQTriggerMetrics> samples, int count, Func<RabbitMQTriggerMetrics, RabbitMQTriggerMetrics, bool> predicate)
    {
        Debug.Assert(count > 1, "count must be greater than 1.");
        Debug.Assert(count <= samples.Count, "count must be less than or equal to the list size.");

        // Walks through the list from left to right starting at len(samples) - count.
        for (int i = samples.Count - count; i < samples.Count - 1; i++)
        {
            if (!predicate(samples[i], samples[i + 1]))
            {
                return false;
            }
        }

        return true;
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(null);
        }
    }

    private ScaleStatus GetScaleStatusCore(int workerCount, RabbitMQTriggerMetrics[] metrics)
    {
        var status = new ScaleStatus
        {
            Vote = ScaleVote.None,
        };

        // TODO: Make the below two ints configurable.
        int numberOfSamplesToConsider = 5;
        int targetQueueLength = 1000;

        if (metrics == null || metrics.Length < numberOfSamplesToConsider)
        {
            return status;
        }

        long latestQueueLength = metrics.Last().QueueLength;

        if (latestQueueLength > workerCount * targetQueueLength)
        {
            status.Vote = ScaleVote.ScaleOut;
            this.logger.LogInformation($"QueueLength ({latestQueueLength}) > workerCount ({workerCount}) * 1000");
            this.logger.LogInformation($"Length of queue ({this.queueName}, {latestQueueLength}) is too high relative to the number of instances ({workerCount}).");
            return status;
        }

        bool queueIsIdle = metrics.All(p => p.QueueLength == 0);

        if (queueIsIdle)
        {
            status.Vote = ScaleVote.ScaleIn;
            this.logger.LogInformation($"Queue '{this.queueName}' is idle");
            return status;
        }

        bool queueLengthIncreasing =
            IsTrueForLast(
                metrics,
                numberOfSamplesToConsider,
                (prev, next) => prev.QueueLength < next.QueueLength) && metrics[0].QueueLength > 0;

        if (queueLengthIncreasing)
        {
            status.Vote = ScaleVote.ScaleOut;
            this.logger.LogInformation($"Queue length is increasing for '{this.queueName}'");
            return status;
        }

        bool queueLengthDecreasing =
            IsTrueForLast(
                metrics,
                numberOfSamplesToConsider,
                (prev, next) => prev.QueueLength > next.QueueLength);

        if (queueLengthDecreasing)
        {
            status.Vote = ScaleVote.ScaleIn;
            this.logger.LogInformation($"Queue length is decreasing for '{this.queueName}'");
        }

        this.logger.LogInformation($"Queue '{this.queueName}' is steady");
        return status;
    }
}
