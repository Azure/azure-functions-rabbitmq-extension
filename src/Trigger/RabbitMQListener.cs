// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
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
        private readonly ushort _batchNumber;
        private readonly IRabbitMQService _service;
        private readonly ILogger _logger;

        private EventingBasicConsumer _consumer;
        private IRabbitMQModel _rabbitMQModel;
        private QueueDeclareOk _queueInfo;
        private List<BasicDeliverEventArgs> batchedMessages = new List<BasicDeliverEventArgs>();

        private string _consumerTag;
        private bool _disposed;
        private bool _started;

        public RabbitMQListener(ITriggeredFunctionExecutor executor, IRabbitMQService service, string queueName, ushort batchNumber, ILogger logger)
        {
            _executor = executor;
            _service = service;
            _queueName = queueName;
            _batchNumber = batchNumber;
            _logger = logger;
            _rabbitMQModel = _service.Model;
            _queueInfo = _service.QueueInfo;
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

            _rabbitMQModel.BasicQos(0, _batchNumber, false);
            _consumer = new EventingBasicConsumer(_rabbitMQModel.Model);

            _consumer.Received += async (model, ea) =>
            {
                FunctionResult result = await _executor.TryExecuteAsync(new TriggeredFunctionData() { TriggerValue = ea }, cancellationToken);

                if (result.Succeeded)
                {
                    _rabbitMQModel.BasicAck(ea.DeliveryTag, true);
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
            _rabbitMQModel.BasicAck(ea.DeliveryTag, false);

            if (ea.BasicProperties.Headers == null)
            {
                ea.BasicProperties.Headers = new Dictionary<string, object>();
            }

            ea.BasicProperties.Headers[Constants.RequeueCount] = 0;
            _logger.LogDebug("Republishing message");
            _rabbitMQModel.BasicPublish(exchange: string.Empty, routingKey: ea.RoutingKey, basicProperties: ea.BasicProperties, body: ea.Body);
        }

        internal void RepublishMessages(BasicDeliverEventArgs ea)
        {
            int requeueCount = Convert.ToInt32(ea.BasicProperties.Headers[Constants.RequeueCount]);
            // Redelivered again
            requeueCount++;
            ea.BasicProperties.Headers[Constants.RequeueCount] = requeueCount;

            if (Convert.ToInt32(ea.BasicProperties.Headers[Constants.RequeueCount]) < 5)
            {
                _rabbitMQModel.BasicAck(ea.DeliveryTag, false); // Manually ACK'ing, but resend
                _logger.LogDebug("Republishing message");
                _rabbitMQModel.BasicPublish(exchange: string.Empty, routingKey: ea.RoutingKey, basicProperties: ea.BasicProperties, body: ea.Body);
            }
            else
            {
                // Add message to dead letter exchange
                _logger.LogDebug("Requeue count exceeded: rejecting message");
                _rabbitMQModel.BasicReject(ea.DeliveryTag, false);
            }
        }

        public string Id
        {
            get
            {
                return $"RabbitMQTrigger-{_queueName}".ToLower();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }
        }

        async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync()
        {
            return await GetMetricsAsync();
        }

        public async Task<RabbitMQTriggerMetrics> GetMetricsAsync()
        {
            return new RabbitMQTriggerMetrics
            {
                QueueLength = _queueInfo.MessageCount,
            };
        }

        ScaleStatus IScaleMonitor.GetScaleStatus(ScaleStatusContext context)
        {
            ScaleStatus status = new ScaleStatus
            {
                Vote = ScaleVote.None,
            };

            const int NumberOfSamplesToConsider = 5;

            List<RabbitMQTriggerMetrics> metrics = (context as ScaleStatusContext<RabbitMQTriggerMetrics>)?.Metrics?.ToList();

            if (metrics == null || metrics.Count < NumberOfSamplesToConsider)
            {
                return status;
            }

            long latestQueueLength = metrics.Last().QueueLength;

            if (latestQueueLength > context.WorkerCount * 1000)
            {
                status.Vote = ScaleVote.ScaleOut;
                _logger.LogInformation($"QueueLength ({latestQueueLength}) > workerCount ({context.WorkerCount}) * 1000");
                _logger.LogInformation($"Length of queue ({_queueInfo.QueueName}, {latestQueueLength}) is too high relative to the number of instances ({context.WorkerCount}).");
                return status;
            }

            bool queueIsIdle = metrics.All(p => p.QueueLength == 0);

            if (queueIsIdle)
            {
                status.Vote = ScaleVote.ScaleIn;
                _logger.LogInformation($"Queue '{_queueInfo.QueueName}' is idle");
                return status;
            }

            _logger.LogInformation($"Queue '{_queueInfo.QueueName}' is steady");
            return status;
        }

        public ScaleStatus GetScaleStatus(ScaleStatusContext<RabbitMQTriggerMetrics> context)
        {
            return ((IScaleMonitor)this).GetScaleStatus(context);
        }
    }
}
