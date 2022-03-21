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
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly string _queueName;
        private readonly ushort _prefetchCount;
        private readonly IRabbitMQService _service;
        private readonly ILogger _logger;
        private readonly string _functionId;
        private readonly IRabbitMQModel _rabbitMQModel;

        private EventingBasicConsumer _consumer;
        private string _consumerTag;
        private bool _disposed;
        private bool _started;

        public RabbitMQListener(
            ITriggeredFunctionExecutor executor,
            IRabbitMQService service,
            string queueName,
            ILogger logger,
            FunctionDescriptor functionDescriptor,
            ushort prefetchCount)
        {
            _executor = executor;
            _service = service;
            _queueName = queueName;
            _logger = logger;
            _rabbitMQModel = _service.RabbitMQModel;
            _ = functionDescriptor ?? throw new ArgumentNullException(nameof(functionDescriptor));
            _functionId = functionDescriptor.Id;
            Descriptor = new ScaleMonitorDescriptor($"{_functionId}-RabbitMQTrigger-{_queueName}".ToLowerInvariant());
            _prefetchCount = prefetchCount;
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
            if (!_started)
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

            if (_started)
            {
                throw new InvalidOperationException("The listener has already been started.");
            }

            _rabbitMQModel.BasicQos(0, _prefetchCount, false);  // Non zero prefetchSize doesn't work (tested upto 5.2.0) and will throw NOT_IMPLEMENTED exception
            _consumer = new EventingBasicConsumer(_rabbitMQModel.Model);

            _consumer.Received += async (model, ea) =>
            {
                var input = new TriggeredFunctionData() { TriggerValue = ea };
                FunctionResult result = await _executor.TryExecuteAsync(input, cancellationToken).ConfigureAwait(false);

                if (result.Succeeded)
                {
                    _rabbitMQModel.BasicAck(ea.DeliveryTag, false);
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

            _consumerTag = _rabbitMQModel.BasicConsume(queue: _queueName, autoAck: false, consumer: _consumer);

            _started = true;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (!_started)
            {
                throw new InvalidOperationException("The listener has not yet been started or has already been stopped");
            }

            _rabbitMQModel.BasicCancel(_consumerTag);
            _rabbitMQModel.Close();
            _started = false;
            _disposed = true;
            return Task.CompletedTask;
        }

        internal void CreateHeadersAndRepublish(BasicDeliverEventArgs ea)
        {
            if (ea.BasicProperties.Headers == null)
            {
                ea.BasicProperties.Headers = new Dictionary<string, object>();
            }

            ea.BasicProperties.Headers[Constants.RequeueCount] = 0;
            _logger.LogDebug("Republishing message");
            _rabbitMQModel.BasicPublish(exchange: string.Empty, routingKey: _queueName, basicProperties: ea.BasicProperties, body: ea.Body);
            _rabbitMQModel.BasicAck(ea.DeliveryTag, false);
        }

        internal void RepublishMessages(BasicDeliverEventArgs ea)
        {
            int requeueCount = Convert.ToInt32(ea.BasicProperties.Headers[Constants.RequeueCount], CultureInfo.InvariantCulture);

            // Redelivered again
            requeueCount++;
            ea.BasicProperties.Headers[Constants.RequeueCount] = requeueCount;

            if (requeueCount < 5)
            {
                _logger.LogDebug("Republishing message");
                _rabbitMQModel.BasicPublish(exchange: string.Empty, routingKey: _queueName, basicProperties: ea.BasicProperties, body: ea.Body);
                _rabbitMQModel.BasicAck(ea.DeliveryTag, false); // Manually ACK'ing, but ack after resend
            }
            else
            {
                // Add message to dead letter exchange
                _logger.LogDebug("Requeue count exceeded: rejecting message");
                _rabbitMQModel.BasicReject(ea.DeliveryTag, false);
            }
        }

        async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync()
        {
            return await GetMetricsAsync().ConfigureAwait(false);
        }

        public Task<RabbitMQTriggerMetrics> GetMetricsAsync()
        {
            QueueDeclareOk queueInfo = _rabbitMQModel.QueueDeclarePassive(_queueName);
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
            if (_disposed)
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
                _logger.LogInformation($"QueueLength ({latestQueueLength}) > workerCount ({workerCount}) * 1000");
                _logger.LogInformation($"Length of queue ({_queueName}, {latestQueueLength}) is too high relative to the number of instances ({workerCount}).");
                return status;
            }

            bool queueIsIdle = metrics.All(p => p.QueueLength == 0);

            if (queueIsIdle)
            {
                status.Vote = ScaleVote.ScaleIn;
                _logger.LogInformation($"Queue '{_queueName}' is idle");
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
                _logger.LogInformation($"Queue length is increasing for '{_queueName}'");
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
                _logger.LogInformation($"Queue length is decreasing for '{_queueName}'");
            }

            _logger.LogInformation($"Queue '{_queueName}' is steady");
            return status;
        }
    }
}
