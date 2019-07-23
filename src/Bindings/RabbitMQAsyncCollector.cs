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
        private readonly RabbitMQContext _context;
        // private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();
        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly QueueDeclareOk _queue;
        private readonly IBasicPublishBatch _messages;

        public RabbitMQAsyncCollector(RabbitMQContext context)
        {
            _context = context;
            _factory = new ConnectionFactory() { HostName = context.Hostname };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            // _queue = CreateQueue(_channel, _context);
            _messages = _channel.CreateBasicPublishBatch();
        }

        public Task AddAsync(string message, CancellationToken cancellationToken = default)
        {
            //message = _context.Message;
            //_messages.Enqueue(message);
            _messages.Add(exchange: string.Empty, routingKey: _context.QueueName, mandatory: false, properties: null, body: Encoding.UTF8.GetBytes(_context.Message));

            return Task.CompletedTask;
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            //while (_messages.TryDequeue(out string message))
            //{
            //    await this.PublishAsync(message);
            //}
            await this.PublishAsync();
        }

        internal Task PublishAsync()
        {
            //var messageBytes = Encoding.UTF8.GetBytes(message);
            //_channel.BasicPublish(exchange: string.Empty, routingKey: _context.QueueName, basicProperties: null, body: messageBytes);

            _messages.Publish();
            return Task.CompletedTask;
        }

        internal static QueueDeclareOk CreateQueue(IModel channel, RabbitMQContext context)
        {
            var response = channel.QueueDeclare(queue: context.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            return response;
        }
    }
}
