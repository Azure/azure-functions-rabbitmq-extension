// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        private IModel _channel;
        private IBasicPublishBatch _batch;

        public RabbitMQService(string hostName, string queueName)
        {
            string host = hostName ?? throw new ArgumentNullException(nameof(hostName));
            string queue = queueName ?? throw new ArgumentNullException(nameof(queueName));
            CreateChannel(host);
            CreateBatch(queue);
        }

        public IModel GetChannel()
        {
            return _channel;
        }

        public IBasicPublishBatch GetBatch()
        {
            return _batch;
        }

        internal void CreateChannel(string hostName)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory() { HostName = hostName };
            _channel = connectionFactory.CreateConnection().CreateModel();
        }

        internal void CreateBatch(string queueName)
        {
            _channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            _batch = _channel.CreateBasicPublishBatch();
        }
    }
}
