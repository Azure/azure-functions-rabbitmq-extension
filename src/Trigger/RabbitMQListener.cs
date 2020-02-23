// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly TriggerConfiguration _options;
        private readonly IRabbitMQService _service;
        private readonly ILogger _logger;
        private readonly FunctionDescriptor _functionDescriptor;
        private readonly string _functionId;
        private readonly List<BasicDeliverEventArgs> batchedMessages = new List<BasicDeliverEventArgs>();
        private readonly ScaleMonitorDescriptor _scaleMonitorDescriptor;

        private AsyncEventingBasicConsumer _consumer;
        private IRabbitMQModel _rabbitMQModel;

        private string _consumerTag;
        private bool _disposed;
        private bool _started;

        public RabbitMQListener(
            ITriggeredFunctionExecutor executor,
            IRabbitMQService service,
            TriggerConfiguration options,
            ILogger logger,
            FunctionDescriptor functionDescriptor)
        {
            _executor = executor;
            _service = service;
            _options = options;
            _logger = logger;
            _rabbitMQModel = _service.RabbitMQModel;
            _functionDescriptor = functionDescriptor ?? throw new ArgumentNullException(nameof(functionDescriptor));
            _functionId = functionDescriptor.Id;
            _scaleMonitorDescriptor = new ScaleMonitorDescriptor($"{_functionId}-RabbitMQTrigger-{_options.Queue.Name}".ToLower());
        }

        public ScaleMonitorDescriptor Descriptor
        {
            get
            {
                return _scaleMonitorDescriptor;
            }
        }

        public void Cancel()
        {
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

            _rabbitMQModel.BasicQos(0, _options.PrefetchCount, false);
            _consumer = new AsyncEventingBasicConsumer(_rabbitMQModel.Model);

            _consumer.Received += async (model, ea) => await OnMessageReceived((AsyncEventingBasicConsumer)model, ea, cancellationToken);

            _consumerTag = _rabbitMQModel.BasicConsume(queue: _options.Queue.Name, autoAck: false, consumer: _consumer);

            _started = true;

            return Task.CompletedTask;
        }

        public async Task OnMessageReceived(AsyncEventingBasicConsumer consumer, BasicDeliverEventArgs ea, CancellationToken token)
        {
            FunctionResult result = await _executor.TryExecuteAsync(new TriggeredFunctionData() { TriggerValue = ea }, token);

            if (result.Succeeded)
            {
                consumer.Model.BasicAck(ea.DeliveryTag, false);
            }
            else
            {
                if (ea.BasicProperties.Headers == null || !ea.BasicProperties.Headers.ContainsKey(Constants.RequeueCount))
                {
                    CreateHeadersAndRepublish(consumer.Model, ea);
                }
                else
                {
                    RepublishMessages(consumer.Model, ea);
                }
            }
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

        internal void CreateHeadersAndRepublish(IModel model, BasicDeliverEventArgs ea)
        {
            model.BasicAck(ea.DeliveryTag, false);

            if (ea.BasicProperties.Headers == null)
            {
                ea.BasicProperties.Headers = new Dictionary<string, object>();
            }

            ea.BasicProperties.Headers[Constants.RequeueCount] = 0;
            _logger.LogDebug("Republishing message");
            model.BasicPublish(exchange: string.Empty, routingKey: ea.RoutingKey, basicProperties: ea.BasicProperties, body: ea.Body);
        }

        internal void RepublishMessages(IModel model, BasicDeliverEventArgs ea)
        {
            int requeueCount = Convert.ToInt32(ea.BasicProperties.Headers[Constants.RequeueCount]);
            // Redelivered again
            requeueCount++;
            ea.BasicProperties.Headers[Constants.RequeueCount] = requeueCount;

            if (Convert.ToInt32(ea.BasicProperties.Headers[Constants.RequeueCount]) < 5)
            {
                model.BasicAck(ea.DeliveryTag, false); // Manually ACK'ing, but resend
                _logger.LogDebug("Republishing message");               
                model.BasicPublish(exchange: string.Empty, routingKey: ea.RoutingKey, basicProperties: ea.BasicProperties, body: ea.Body);
            }
            else
            {
                // Add message to dead letter exchange
                _logger.LogDebug("Requeue count exceeded: rejecting message");
                model.BasicReject(ea.DeliveryTag, false);
            }
        }

        async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync()
        {
            return await GetMetricsAsync();
        }

        public Task<RabbitMQTriggerMetrics> GetMetricsAsync()
        {
            QueueDeclareOk queueInfo = _rabbitMQModel.QueueDeclarePassive(_options.Queue.Name);
            RabbitMQTriggerMetrics metrics = new RabbitMQTriggerMetrics
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
            ScaleStatus status = new ScaleStatus
            {
                Vote = ScaleVote.None,
            };

            const int NumberOfSamplesToConsider = 5;

            if (metrics == null || metrics.Length < NumberOfSamplesToConsider)
            {
                return status;
            }

            long latestQueueLength = metrics.Last().QueueLength;

            if (latestQueueLength > workerCount * 1000)
            {
                status.Vote = ScaleVote.ScaleOut;
                _logger.LogInformation($"QueueLength ({latestQueueLength}) > workerCount ({workerCount}) * 1000");
                _logger.LogInformation($"Length of queue ({_options.Queue.Name}, {latestQueueLength}) is too high relative to the number of instances ({workerCount}).");
                return status;
            }

            bool queueIsIdle = metrics.All(p => p.QueueLength == 0);

            if (queueIsIdle)
            {
                status.Vote = ScaleVote.ScaleIn;
                _logger.LogInformation($"Queue '{_options.Queue.Name}' is idle");
                return status;
            }

            bool queueLengthIncreasing =
                IsTrueForLast(
                    metrics,
                    NumberOfSamplesToConsider,
                    (prev, next) => prev.QueueLength < next.QueueLength) && metrics[0].QueueLength > 0;

            if (queueLengthIncreasing)
            {
                status.Vote = ScaleVote.ScaleOut;
                _logger.LogInformation($"Queue length is increasing for '{_options.Queue.Name}'");
                return status;
            }

            bool queueLengthDecreasing =
                IsTrueForLast(
                    metrics,
                    NumberOfSamplesToConsider,
                    (prev, next) => prev.QueueLength > next.QueueLength);

            if (queueLengthDecreasing)
            {
                status.Vote = ScaleVote.ScaleIn;
                _logger.LogInformation($"Queue length is decreasing for '{_options.Queue.Name}'");
            }

            _logger.LogInformation($"Queue '{_options.Queue.Name}' is steady");
            return status;
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
    }
}
