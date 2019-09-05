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
        private readonly IBasicPublishBatch _batch;

        private readonly ILogger _logger;

        public RabbitMQAsyncCollector(RabbitMQContext context, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            if (_context.Service == null)
            {
                throw new ArgumentNullException("context.service");
            }

            _batch = _context.Service.BasicPublishBatch;
        }

        public Task AddAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            AddAsync(message, _context.ResolvedAttribute.QueueName);

            return Task.CompletedTask;
        }

        public Task AddAsync(byte[] message, string routingKey, CancellationToken cancellationToken = default)
        {
            _batch.Add(exchange: string.Empty, routingKey: routingKey, mandatory: false, properties: null, body: message);
            _logger.LogDebug($"Adding message to batch for publishing...");

            return Task.CompletedTask;
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await PublishAsync();
        }

        internal Task PublishAsync()
        {
            _batch.Publish();
            _logger.LogDebug($"Publishing messages to queue.");
            return Task.CompletedTask;
        }
    }
}
