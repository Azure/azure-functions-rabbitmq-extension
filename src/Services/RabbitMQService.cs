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
            _channel = CreateChannel(hostname, queuename);
            _batch = CreateBatch(queuename);
        }

        public IModel CreateChannel(string hostname, string queuename)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory() { HostName = hostname };
            IModel channel = connectionFactory.CreateConnection().CreateModel();

            return channel;
        }

        public IBasicPublishBatch CreateBatch(string queuename)
        {
            _channel.QueueDeclare(queue: queuename, durable: false, exclusive: false, autoDelete: false, arguments: null);
            return _channel.CreateBasicPublishBatch();
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
