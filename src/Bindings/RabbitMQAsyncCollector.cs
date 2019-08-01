// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class RabbitMQAsyncCollector : IAsyncCollector<byte[]>
    {
        private readonly RabbitMQContext _context;
        private readonly IModel _channel;
        private readonly QueueDeclareOk _queue;
        private readonly IBasicPublishBatch _batch;

        private ILogger<RabbitMQAsyncCollector> _logger;

        public RabbitMQAsyncCollector(RabbitMQContext context, ILogger<RabbitMQAsyncCollector> logger)
        {
            _context = context;
            _channel = _context.Service.GetChannel();
            _queue = _channel.QueueDeclare(queue: _context.ResolvedAttribute.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            _batch = _channel.CreateBasicPublishBatch();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal RabbitMQContext Context => _context;

        public Task AddAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            _batch.Add(exchange: _context.ResolvedAttribute.Exchange, routingKey: _context.ResolvedAttribute.QueueName, mandatory: false, properties: _context.ResolvedAttribute.Properties, body: message);
            _logger.LogDebug($"Message: {message}, Queue Name: {_context.ResolvedAttribute.QueueName}");
            _logger.LogDebug($"Adding message to batch for publishing...");

            return Task.CompletedTask;
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.PublishAsync();
        }

        internal Task PublishAsync()
        {
            _batch.Publish();
            _logger.LogDebug($"Publishing messages to queue. Number of messages: {_queue.MessageCount}");
            return Task.CompletedTask;
        }
    }
}
