// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        private IBasicPublishBatch _batch;

        public RabbitMQService(string hostname, string queuename)
        {
            _batch = CreateBatch(hostname, queuename);
        }

        public IBasicPublishBatch CreateBatch(string hostname, string queuename)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory() { HostName = hostname };
            IModel channel = connectionFactory.CreateConnection().CreateModel();
            channel.QueueDeclare(queue: queuename, durable: false, exclusive: false, autoDelete: false, arguments: null);
            return channel.CreateBasicPublishBatch();
        }

        public IBasicPublishBatch GetBatch()
        {
            return _batch;
        }
    }
}
