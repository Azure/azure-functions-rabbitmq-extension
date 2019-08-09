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
        private string _userName;
        private string _password;
        private int _port;

        public RabbitMQService(string hostName, string queueName, string userName, string password, int port)
        {
            _hostName = hostName ?? throw new ArgumentNullException(nameof(hostName));
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            _userName = userName ?? "guest";
            _password = password ?? "guest";
            _port = port;

            ConnectionFactory connectionFactory = new ConnectionFactory()
            {
                HostName = _hostName,
                UserName = _userName,
                Password = _password,
            };

            // Only set port if it's specified by the user
            if (_port != 0)
            {
                connectionFactory.Port = _port;
            }

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
