// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal sealed class RabbitMQListener : IListener, IScaleMonitor<RabbitMQTriggerMetrics>
{
    private const int ListenerNotStarted = 0;
    private const int ListenerStarting = 1;
    private const int ListenerStarted = 2;
    private const int ListenerStopping = 3;
    private const int ListenerStopped = 4;

    private const string RequeueCountHeaderName = "x-ms-rabbitmq-requeuecount";

    private readonly ITriggeredFunctionExecutor executor;
    private readonly IModel channel;
    private readonly string queueName;
    private readonly ushort prefetchCount;
    private readonly ILogger logger;

    private readonly string logdetails;

    private int listenerState = ListenerNotStarted;

    private string consumerTag;

    public RabbitMQListener(
        ITriggeredFunctionExecutor executor,
        string functionId,
        IModel channel,
        string queueName,
        ushort prefetchCount,
        ILogger logger)
    {
        this.executor = executor;
        this.channel = channel;
        this.queueName = queueName;
        this.prefetchCount = prefetchCount;
        this.logger = logger;

        this.logdetails = $"function: '{functionId}, queue: '{queueName}'";

        this.Descriptor = new ScaleMonitorDescriptor($"{functionId}-RabbitMQTrigger-{queueName}".ToLowerInvariant());
    }

    public ScaleMonitorDescriptor Descriptor { get; }

    public void Cancel()
    {
        this.StopAsync(CancellationToken.None).Wait();
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        int previousState = Interlocked.CompareExchange(ref this.listenerState, ListenerStarting, ListenerNotStarted);

        if (previousState != ListenerNotStarted)
        {
            throw new InvalidOperationException("The listener is either starting or has already started.");
        }

        // Non-zero prefetch size doesn't work (tested upto 5.2.0) and will throw NOT_IMPLEMENTED exception.
        this.channel.BasicQos(prefetchSize: 0, this.prefetchCount, global: false);
        var consumer = new EventingBasicConsumer(this.channel);

        consumer.Received += async (model, args) =>
        {
            // The RabbitMQ client rents an array from the ArrayPool to hold a copy of the message body, and passes it
            // to the listener. Once all event handlers are executed, the array is returned back to the pool so that the
            // memory can be reused for future messages for that connection. However, since our event handler is async,
            // the very first await statement i.e. the call to TryExecuteAsync below causes the event handler invocation
            // to complete and lets the RabbitMQ client release the memory. This led to message body corruption when the
            // message is republished (see: https://github.com/Azure/azure-functions-rabbitmq-extension/issues/211).
            //
            // We chose to copy the message body instead of having a new 'args' object as there is only one event
            // handler registered for the consumer so there should be no side-effects.
            args.Body = args.Body.ToArray();

            using Activity activity = RabbitMQActivitySource.StartActivity(args.BasicProperties);

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
                    this.logger.LogDebug($"Rejecting message since requeue count exceeded for {this.logdetails}.");
                    this.channel.BasicReject(args.DeliveryTag, requeue: false);
                    return;
                }

                this.logger.LogDebug($"Republishing message for {this.logdetails}.");
                args.BasicProperties.Headers[RequeueCountHeaderName] = requeueCount;

                // TODO: Check if 'BasicReject' with requeue = true would work here.
                this.channel.BasicPublish(exchange: string.Empty, routingKey: this.queueName, args.BasicProperties, args.Body);
            }

            this.channel.BasicAck(args.DeliveryTag, multiple: false);
        };

        this.consumerTag = this.channel.BasicConsume(queue: this.queueName, autoAck: false, consumer);

        this.listenerState = ListenerStarted;
        this.logger.LogDebug($"Started RabbitMQ trigger listener for {this.logdetails}.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        int previousState = Interlocked.CompareExchange(ref this.listenerState, ListenerStopping, ListenerStarted);

        if (previousState == ListenerStarted)
        {
            this.channel.BasicCancel(this.consumerTag);
            this.channel.Close();

            this.listenerState = ListenerStopped;
            this.logger.LogDebug($"Stopped RabbitMQ trigger listener for {this.logdetails}.");
        }

        return Task.CompletedTask;
    }

    async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync()
    {
        return await this.GetMetricsAsync().ConfigureAwait(false);
    }

    public Task<RabbitMQTriggerMetrics> GetMetricsAsync()
    {
        QueueDeclareOk queueInfo = this.channel.QueueDeclarePassive(this.queueName);
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
