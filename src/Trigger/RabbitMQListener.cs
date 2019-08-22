// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQListener : IListener
    {
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly string _queueName;
        private readonly ushort _batchNumber;
        private readonly IRabbitMQService _service;
        private readonly ILogger _logger;

        private EventingBasicConsumer _consumer;
        private IModel _model;
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

            _model = _service.Model;
            _model.BasicQos(0, _batchNumber, false);
            _consumer = new EventingBasicConsumer(_model);

            _consumer.Received += async (model, ea) =>
            {
                FunctionResult result = await _executor.TryExecuteAsync(new TriggeredFunctionData() { TriggerValue = ea }, cancellationToken);

                if (result.Succeeded)
                {
                    _model.BasicAck(ea.DeliveryTag, true);
                }
                else
                {
                    if (ea.BasicProperties.Headers == null || !ea.BasicProperties.Headers.ContainsKey("requeueCount"))
                    {
                        CreateHeadersAndRepublish(ea);
                    }
                    else
                    {
                        RepublishMessages(ea);
                    }
                }
            };

            _consumerTag = _model.BasicConsume(queue: _queueName, autoAck: false, consumer: _consumer);

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

            _model.BasicCancel(_consumerTag);
            _model.Close();
            _started = false;
            _disposed = true;
            return Task.CompletedTask;
        }

        private void CreateHeadersAndRepublish(BasicDeliverEventArgs ea)
        {
            _model.BasicAck(ea.DeliveryTag, false);

            if (ea.BasicProperties.Headers == null) {
                ea.BasicProperties.Headers = new Dictionary<string, object>();
            }

            ea.BasicProperties.Headers["requeueCount"] = 0;
            _logger.LogInformation("Republishing message {0}", Encoding.UTF8.GetString(ea.Body));
            _model.BasicPublish(exchange: string.Empty, routingKey: ea.RoutingKey, basicProperties: ea.BasicProperties, body: ea.Body);
        }

        private void RepublishMessages(BasicDeliverEventArgs ea)
        {
            int requeueCount = Convert.ToInt32(ea.BasicProperties.Headers["requeueCount"]);
            // Redelivered again
            requeueCount++;
            ea.BasicProperties.Headers["requeueCount"] = requeueCount;

            if (Convert.ToInt32(ea.BasicProperties.Headers["requeueCount"]) < 5)
            {
                _model.BasicAck(ea.DeliveryTag, false); // Manually ACK'ing, but resend
                _logger.LogInformation("Republishing message {0}", Encoding.UTF8.GetString(ea.Body));
                _model.BasicPublish(exchange: string.Empty, routingKey: ea.RoutingKey, basicProperties: ea.BasicProperties, body: ea.Body);
            }
            else
            {
                // Add message to dead letter exchange
                _logger.LogInformation("Adding message '{0}' to dead letter exchange", Encoding.UTF8.GetString(ea.Body));
                _model.BasicReject(ea.DeliveryTag, false);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }
        }
    }
}
