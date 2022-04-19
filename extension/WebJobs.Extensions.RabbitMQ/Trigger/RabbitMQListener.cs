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
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQListener : IListener, IScaleMonitor<RabbitMQTriggerMetrics>
    {
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
            rabbitMQModel = this.service.RabbitMQModel;
            _ = functionDescriptor ?? throw new ArgumentNullException(nameof(functionDescriptor));
            functionId = functionDescriptor.Id;
            Descriptor = new ScaleMonitorDescriptor($"{functionId}-RabbitMQTrigger-{this.queueName}".ToLowerInvariant());
            this.prefetchCount = prefetchCount;
        }

        public ScaleMonitorDescriptor Descriptor { get; }

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

        public void Cancel()
        {
            if (!started)
            {
                return;
            }

            StopAsync(CancellationToken.None).Wait();
        }

        public void Dispose()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (started)
            {
                throw new InvalidOperationException("The listener has already been started.");
            }

            rabbitMQModel.BasicQos(0, prefetchCount, false);  // Non zero prefetchSize doesn't work (tested upto 5.2.0) and will throw NOT_IMPLEMENTED exception
            consumer = new EventingBasicConsumer(rabbitMQModel.Model);

            consumer.Received += async (model, ea) =>
            {
                var input = new TriggeredFunctionData() { TriggerValue = ea };
                FunctionResult result = await executor.TryExecuteAsync(input, cancellationToken).ConfigureAwait(false);

                if (result.Succeeded)
                {
                    rabbitMQModel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    if (ea.BasicProperties.Headers == null || !ea.BasicProperties.Headers.ContainsKey(Constants.RequeueCount))
                    {
                        CreateHeadersAndRepublish(ea);
                    }
                    else
                    {
                        RepublishMessages(ea);
                    }
                }
            };

            consumerTag = rabbitMQModel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            started = true;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (!started)
            {
                throw new InvalidOperationException("The listener has not yet been started or has already been stopped");
            }

            rabbitMQModel.BasicCancel(consumerTag);
            rabbitMQModel.Close();
            started = false;
            disposed = true;
            return Task.CompletedTask;
        }

        internal void CreateHeadersAndRepublish(BasicDeliverEventArgs ea)
        {
            if (ea.BasicProperties.Headers == null)
            {
                ea.BasicProperties.Headers = new Dictionary<string, object>();
            }

            ea.BasicProperties.Headers[Constants.RequeueCount] = 0;
            logger.LogDebug("Republishing message");
            rabbitMQModel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: ea.BasicProperties, body: ea.Body);
            rabbitMQModel.BasicAck(ea.DeliveryTag, false);
        }

        internal void RepublishMessages(BasicDeliverEventArgs ea)
        {
            int requeueCount = Convert.ToInt32(ea.BasicProperties.Headers[Constants.RequeueCount], CultureInfo.InvariantCulture);

            // Redelivered again
            requeueCount++;
            ea.BasicProperties.Headers[Constants.RequeueCount] = requeueCount;

            if (requeueCount < 5)
            {
                logger.LogDebug("Republishing message");
                rabbitMQModel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: ea.BasicProperties, body: ea.Body);
                rabbitMQModel.BasicAck(ea.DeliveryTag, false); // Manually ACK'ing, but ack after resend
            }
            else
            {
                // Add message to dead letter exchange
                logger.LogDebug("Requeue count exceeded: rejecting message");
                rabbitMQModel.BasicReject(ea.DeliveryTag, false);
            }
        }

        async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync()
        {
            return await GetMetricsAsync().ConfigureAwait(false);
        }

        public Task<RabbitMQTriggerMetrics> GetMetricsAsync()
        {
            QueueDeclareOk queueInfo = rabbitMQModel.QueueDeclarePassive(queueName);
            var metrics = new RabbitMQTriggerMetrics
            {
                QueueLength = queueInfo.MessageCount,
                Timestamp = DateTime.UtcNow,
            };

            return Task.FromResult(metrics);
        }

        ScaleStatus IScaleMonitor.GetScaleStatus(ScaleStatusContext context)
        {
            return GetScaleStatusCore(context.WorkerCount, context.Metrics?.Cast<RabbitMQTriggerMetrics>().ToArray());
        }

        public ScaleStatus GetScaleStatus(ScaleStatusContext<RabbitMQTriggerMetrics> context)
        {
            return GetScaleStatusCore(context.WorkerCount, context.Metrics?.ToArray());
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
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
                logger.LogInformation($"QueueLength ({latestQueueLength}) > workerCount ({workerCount}) * 1000");
                logger.LogInformation($"Length of queue ({queueName}, {latestQueueLength}) is too high relative to the number of instances ({workerCount}).");
                return status;
            }

            bool queueIsIdle = metrics.All(p => p.QueueLength == 0);

            if (queueIsIdle)
            {
                status.Vote = ScaleVote.ScaleIn;
                logger.LogInformation($"Queue '{queueName}' is idle");
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
                logger.LogInformation($"Queue length is increasing for '{queueName}'");
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
                logger.LogInformation($"Queue length is decreasing for '{queueName}'");
            }

            logger.LogInformation($"Queue '{queueName}' is steady");
            return status;
        }
    }
}
