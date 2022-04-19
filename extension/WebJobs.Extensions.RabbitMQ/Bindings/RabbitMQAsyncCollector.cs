﻿// Copyright (c) .NET Foundation. All rights reserved.
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
        private readonly RabbitMQContext context;
        private readonly ILogger logger;

        public RabbitMQAsyncCollector(RabbitMQContext context, ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = context ?? throw new ArgumentNullException(nameof(context));
            _ = context.Service ?? throw new ArgumentException("Value cannot be null. Parameter name: context.Service");
            this.context = context;
        }

        public Task AddAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Adding message to batch for publishing...");

            lock (context.Service.PublishBatchLock)
            {
                context.Service.BasicPublishBatch.Add(exchange: string.Empty, routingKey: context.ResolvedAttribute.QueueName, mandatory: false, properties: null, body: message);
            }

            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return PublishAsync();
        }

        internal Task PublishAsync()
        {
            logger.LogDebug("Publishing messages to queue.");

            lock (context.Service.PublishBatchLock)
            {
                context.Service.BasicPublishBatch.Publish();
                context.Service.ResetPublishBatch();
            }

            return Task.CompletedTask;
        }
    }
}
