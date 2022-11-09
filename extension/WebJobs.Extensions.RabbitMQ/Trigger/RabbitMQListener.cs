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

    private readonly IModel channel;
    private readonly ITriggeredFunctionExecutor executor;
    private readonly ILogger logger;
    private readonly string queueName;
    private readonly ushort prefetchCount;
    private readonly string logDetails;

    private int listenerState = ListenerNotStarted;
    private string consumerTag;

    public RabbitMQListener(
        IModel channel,
        ITriggeredFunctionExecutor executor,
        ILogger logger,
        string functionId,
        string queueName,
        ushort prefetchCount)
    {
        this.channel = channel ?? throw new ArgumentNullException(nameof(channel));
        this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.queueName = !string.IsNullOrWhiteSpace(queueName) ? queueName : throw new ArgumentNullException(nameof(queueName));
        this.prefetchCount = prefetchCount;

        _ = !string.IsNullOrWhiteSpace(functionId) ? true : throw new ArgumentNullException(nameof(functionId));

        // Do not convert the scale-monitor ID to lower-case string since RabbitMQ queue names are case-sensitive.
        this.Descriptor = new ScaleMonitorDescriptor($"{functionId}-RabbitMQTrigger-{queueName}");
        this.logDetails = $"function: '{functionId}', queue: '{queueName}'";
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

        // It is possible that the WebJobs SDKS invokes StartAsync() method more than once, if there are other trigger
        // listeners registered and some of them have failed to start.
        if (previousState != ListenerNotStarted)
        {
            throw new InvalidOperationException("The listener is either starting or has already started.");
        }

        // The RabbitMQ server (v3.11.2 as of latest) only has support for prefetch size of zero (no specific limit).
        // See: https://github.com/rabbitmq/rabbitmq-server/blob/v3.11.2/deps/rabbit/src/rabbit_channel.erl#L1543.
        // See: https://www.rabbitmq.com/amqp-0-9-1-reference.html#basic.qos.prefetch-size for protocol specification.
        this.channel.BasicQos(prefetchSize: 0, this.prefetchCount, global: false);

        // We should use AsyncEventingBasicConsumer to create the consumer since our handler method is async. Using
        // EventingBasicConsumer led to issue: https://github.com/Azure/azure-functions-rabbitmq-extension/issues/211).
        var consumer = new AsyncEventingBasicConsumer(this.channel);
        consumer.Received += ReceivedHandler;

        this.consumerTag = this.channel.BasicConsume(this.queueName, autoAck: false, consumer);

        this.listenerState = ListenerStarted;
        this.logger.LogDebug($"Started RabbitMQ trigger listener for {this.logDetails}.");

        return Task.CompletedTask;

        async Task ReceivedHandler(object model, BasicDeliverEventArgs args)
        {
            using Activity activity = RabbitMQActivitySource.StartActivity(args.BasicProperties);

            var input = new TriggeredFunctionData() { TriggerValue = args };
            FunctionResult result = await this.executor.TryExecuteAsync(input, cancellationToken).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                // Retry by republishing a copy of message to the queue if the triggered function failed to run.
                args.BasicProperties.Headers ??= new Dictionary<string, object>();
                args.BasicProperties.Headers.TryGetValue(RequeueCountHeaderName, out object headerValue);
                int requeueCount = Convert.ToInt32(headerValue, CultureInfo.InvariantCulture) + 1;

                if (requeueCount >= 5)
                {
                    // Discard or 'dead-letter' the message. See: https://www.rabbitmq.com/dlx.html.
                    this.logger.LogDebug($"Rejecting message since the requeue count exceeded for {this.logDetails}.");
                    this.channel.BasicReject(args.DeliveryTag, requeue: false);
                    return;
                }

                this.logger.LogDebug($"Republishing message for {this.logDetails}.");
                args.BasicProperties.Headers[RequeueCountHeaderName] = requeueCount;

                // We cannot call BasicReject() on the message with requeue = true since that would not enable a fixed
                // number of retry attempts. See: https://stackoverflow.com/q/23158310.
                this.channel.BasicPublish(exchange: string.Empty, routingKey: this.queueName, args.BasicProperties, args.Body);
            }

            // Acknowledge the existing message only after the message (in case of failure) is re-published.
            this.channel.BasicAck(args.DeliveryTag, multiple: false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        int previousState = Interlocked.CompareExchange(ref this.listenerState, ListenerStopping, ListenerStarted);

        if (previousState == ListenerStarted)
        {
            // TODO: Close RabbitMQ connection along with the channel.
            this.channel.BasicCancel(this.consumerTag);
            this.channel.Close();

            this.listenerState = ListenerStopped;
            this.logger.LogDebug($"Stopped RabbitMQ trigger listener for {this.logDetails}.");
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
            MessageCount = queueInfo.MessageCount,
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

    /// <summary>
    /// Returns scale recommendation i.e. whether to scale in or out the host application. The recommendation is
    /// made based on both the latest metrics and the trend of increase or decrease in the the number of messages in
    /// Ready state in the queue. In all of the calculations, it is attempted to keep the number of workers minimum
    /// while also ensuring that the message count per worker stays under the maximum limit.
    /// </summary>
    /// <param name="workerCount">The current worker count for the host application.</param>
    /// <param name="metrics">The collection of metrics samples to make the scale decision.</param>
    private ScaleStatus GetScaleStatusCore(int workerCount, RabbitMQTriggerMetrics[] metrics)
    {
        // We require minimum 5 samples to estimate the trend of variations in message count with certain reliability.
        // These samples roughly cover the timespan of past 40 seconds.
        int minSamplesForScaling = 5;

        // TODO: Make this value configurable.
        // Upper limit on the count of messages that needs to be maintained per worker.
        int maxMessagesPerWorker = 1000;

        var status = new ScaleStatus
        {
            Vote = ScaleVote.None,
        };

        // Do not make a scale decision unless we have enough samples.
        if (metrics == null || metrics.Length < minSamplesForScaling)
        {
            this.logger.LogInformation($"Requesting no-scaling: Insufficient metrics for making scale decision for {this.logDetails}.");
            return status;
        }

        // Consider only the most recent batch of samples in the rest of the method.
        metrics = metrics.Skip(metrics.Length - minSamplesForScaling).ToArray();

        string counts = string.Join(", ", metrics.Select(metric => metric.MessageCount));
        this.logger.LogInformation($"Message counts: [{counts}], worker count: {workerCount}, maximum messages per worker: {maxMessagesPerWorker}.");

        // Add worker if the count of messages per worker exceeds the maximum limit.
        long lastMessageCount = metrics.Last().MessageCount;
        if (lastMessageCount > workerCount * maxMessagesPerWorker)
        {
            status.Vote = ScaleVote.ScaleOut;
            this.logger.LogInformation($"Requesting scale-out: Found too many messages for {this.logDetails} relative to the number of workers.");
            return status;
        }

        // Check if there is a continuous increase or decrease in the count of messages.
        bool isIncreasing = true;
        bool isDecreasing = true;
        for (int index = 0; index < metrics.Length - 1; index++)
        {
            isIncreasing = isIncreasing && metrics[index].MessageCount < metrics[index + 1].MessageCount;
            isDecreasing = isDecreasing && (metrics[index + 1].MessageCount == 0 || metrics[index].MessageCount > metrics[index + 1].MessageCount);
        }

        if (isIncreasing)
        {
            // Scale out only if the expected count of messages would exceed the combined limit after 30 seconds.
            DateTime referenceTime = metrics[metrics.Length - 1].Timestamp - TimeSpan.FromSeconds(30);
            RabbitMQTriggerMetrics referenceMetric = metrics.First(metric => metric.Timestamp > referenceTime);
            long expectedMessageCount = (2 * metrics[metrics.Length - 1].MessageCount) - referenceMetric.MessageCount;

            if (expectedMessageCount > workerCount * maxMessagesPerWorker)
            {
                status.Vote = ScaleVote.ScaleOut;
                this.logger.LogInformation($"Requesting scale-out: Found the messages for {this.logDetails} to be continuously increasing" +
                    " and may exceed the maximum limit set for the workers.");
                return status;
            }
            else
            {
                this.logger.LogDebug($"Avoiding scale-out: Found the messages for {this.logDetails} to be increasing" +
                    " but they may not exceed the maximum limit set for the workers.");
            }
        }

        if (isDecreasing)
        {
            // Scale in only if the count of messages will not exceed the combined limit post the scale-in operation.
            if (lastMessageCount <= (workerCount - 1) * maxMessagesPerWorker)
            {
                status.Vote = ScaleVote.ScaleIn;
                this.logger.LogInformation($"Requesting scale-in: Found {this.logDetails} to be either idle or the messages to be continuously decreasing.");
                return status;
            }
            else
            {
                this.logger.LogDebug($"Avoiding scale-in: Found the messages for {this.logDetails} to be decreasing" +
                    " but they are high enough to require all existing workers for processing.");
            }
        }

        this.logger.LogInformation($"Requesting no-scaling: Found {this.logDetails} to not require scaling.");
        return status;
    }
}
