// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
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

        private EventingBasicConsumer _consumer;
        private IModel _channel;
        private List<BasicDeliverEventArgs> batchedMessages = new List<BasicDeliverEventArgs>();

        private string _consumerTag;
        private bool _disposed;
        private bool _started;

        public RabbitMQListener(ITriggeredFunctionExecutor executor, IRabbitMQService service, string queueName, ushort batchNumber)
        {
            _executor = executor;
            _service = service;
            _queueName = queueName;
            _batchNumber = batchNumber;
        }

        public void Cancel()
        {
            this.StopAsync(CancellationToken.None).Wait();
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

            _channel = _service.GetChannel();
            _channel.BasicQos(0, _batchNumber, false);
            _consumer = new EventingBasicConsumer(_channel);

            _consumer.Received += (model, ea) =>
            {
                // Requeues unacknowledged messages.
                _channel.BasicNack(ea.DeliveryTag, true, true);
                if (_batchNumber == 1)
                {
                    _executor.TryExecuteAsync(new TriggeredFunctionData() { TriggerValue = ea }, cancellationToken);
                }
                else if (batchedMessages.Count >= _batchNumber)
                {
                    batchedMessages.Add(ea);
                    _executor.TryExecuteAsync(new TriggeredFunctionData() { TriggerValue = batchedMessages }, cancellationToken);
                }
                else
                {
                    batchedMessages.Add(ea);
                }
            };

            _consumerTag = _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: _consumer);

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

            _channel.BasicCancel(_consumerTag);
            _channel.Close();
            _started = false;
            _disposed = true;
            return Task.CompletedTask;
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
