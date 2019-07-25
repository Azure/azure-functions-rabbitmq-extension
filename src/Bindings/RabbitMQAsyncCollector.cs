// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class RabbitMQAsyncCollector : IAsyncCollector<byte[]>
    {
        private readonly RabbitMQContext _context;
        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly QueueDeclareOk _queue;
        private readonly IBasicPublishBatch _messages;
        private ILogger _logger;

        public RabbitMQAsyncCollector(RabbitMQContext context, ILogger logger)
        {
            _context = context;
            _factory = new ConnectionFactory() { HostName = context.Hostname };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _queue = CreateQueue(_channel, _context);
            _messages = _channel.CreateBasicPublishBatch();
            _logger = logger;
        }

        public Task AddAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            _messages.Add(exchange: _context.Exchange, routingKey: _context.QueueName, mandatory: false, properties: _context.Properties, body: message);
            _logger.LogDebug($"Adding message to batch for publishing...");

            return Task.CompletedTask;
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.PublishAsync();
        }

        internal Task PublishAsync()
        {
            _messages.Publish();
            _logger.LogDebug($"Publishing messages to queue...");
            return Task.CompletedTask;
        }

        internal static QueueDeclareOk CreateQueue(IModel channel, RabbitMQContext context)
        {
            var response = channel.QueueDeclare(queue: context.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            return response;
        }
    }
}
