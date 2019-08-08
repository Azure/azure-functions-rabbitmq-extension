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
        private string _hostName;
        private string _queueName;

        public RabbitMQService(string hostName, string queueName)
        {
            _hostName = hostName ?? throw new ArgumentNullException(nameof(hostName));
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

            ConnectionFactory connectionFactory = new ConnectionFactory() { HostName = _hostName };
            _channel = connectionFactory.CreateConnection().CreateModel();

            _channel.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            _batch = _channel.CreateBasicPublishBatch();
        }

        public IModel GetChannel()
        {
            return _channel;
        }

        public IBasicPublishBatch GetBatch()
        {
            return _batch;
        }
    }
}
