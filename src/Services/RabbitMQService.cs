// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        private IModel _channel;
        private IBasicPublishBatch _batch;

        public RabbitMQService(string hostname, string queuename)
        {
            CreateChannel(hostname);
            CreateBatch(queuename);
        }

        public IModel GetChannel()
        {
            return _channel;
        }

        public IBasicPublishBatch GetBatch()
        {
            return _batch;
        }

        internal void CreateChannel(string hostname)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory() { HostName = hostname };
            _channel = connectionFactory.CreateConnection().CreateModel();
        }

        internal void CreateBatch(string queuename)
        {
            _channel.QueueDeclare(queue: queuename, durable: false, exclusive: false, autoDelete: false, arguments: null);
            _batch = _channel.CreateBasicPublishBatch();
        }
    }
}
