// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class RabbitMQAsyncCollector : IAsyncCollector<string>
    {
        private readonly RabbitMQContext context;
        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private readonly ConnectionFactory factory;
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly QueueDeclareOk queue;

        public RabbitMQAsyncCollector(RabbitMQContext context)
        {
            this.context = context;
            this.factory = new ConnectionFactory() { HostName = context.Hostname };
            this.connection = this.factory.CreateConnection();
            this.channel = this.connection.CreateModel();
            this.queue = CreateQueue(this.channel, this.context);
        }

        public Task AddAsync(string message, CancellationToken cancellationToken = default)
        {
            message = this.context.Message;
            this.messages.Enqueue(message);

            return Task.CompletedTask;
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            while (this.messages.TryDequeue(out string message))
            {
                await this.PublishAsync(message);
            }
        }

        public Task PublishAsync(string message)
        {
            Console.WriteLine(message);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            this.channel.BasicPublish(exchange: string.Empty, routingKey: this.context.QueueName, basicProperties: null, body: messageBytes);
            return Task.CompletedTask;
        }

        internal static QueueDeclareOk CreateQueue(IModel channel, RabbitMQContext context)
        {
            var response = channel.QueueDeclare(queue: context.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            return response;
        }
    }
}
