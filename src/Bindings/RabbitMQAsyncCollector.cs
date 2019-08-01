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
        private ILogger<RabbitMQAsyncCollector> _logger;

        public RabbitMQAsyncCollector(RabbitMQContext context, ILogger<RabbitMQAsyncCollector> logger)
        {
            _context = context;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task AddAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            _context.Batch.Add(exchange: _context.ResolvedAttribute.Exchange, routingKey: _context.ResolvedAttribute.QueueName, mandatory: false, properties: _context.ResolvedAttribute.Properties, body: message);
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
            _context.Batch.Publish();
            _logger.LogDebug($"Publishing messages to queue. Number of messages: {_context.Queue.MessageCount}");
            return Task.CompletedTask;
        }
    }
}
