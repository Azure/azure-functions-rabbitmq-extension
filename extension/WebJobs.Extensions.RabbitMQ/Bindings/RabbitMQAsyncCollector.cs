// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class RabbitMQAsyncCollector : IAsyncCollector<ReadOnlyMemory<byte>>
    {
        private readonly RabbitMQContext _context;
        private readonly ILogger _logger;

        public RabbitMQAsyncCollector(RabbitMQContext context, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            if (_context.Service == null)
            {
                throw new ArgumentNullException("context.service");
            }
        }

        public Task AddAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"Adding message to batch for publishing...");

            lock (_context.Service.PublishBatchLock)
            {
                _context.Service.BasicPublishBatch.Add(exchange: string.Empty, routingKey: _context.ResolvedAttribute.QueueName, mandatory: false, properties: null, body: message);
            }

            return Task.CompletedTask;
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await PublishAsync();
        }

        internal Task PublishAsync()
        {
            _logger.LogDebug($"Publishing messages to queue.");

            lock (_context.Service.PublishBatchLock)
            {
                _context.Service.BasicPublishBatch.Publish();
                _context.Service.ResetPublishBatch();
            }

            return Task.CompletedTask;
        }
    }
}
