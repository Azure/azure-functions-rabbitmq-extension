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
        private string _connectionString;
        private string _hostName;
        private string _queueName;
        private string _userName;
        private string _password;
        private int _port;

        public RabbitMQService(string connectionString, string hostName, string queueName, string userName, string password, int port)
        {
            _connectionString = connectionString;
            _hostName = hostName;
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            _userName = userName;
            _password = password;
            _port = port;

            ConnectionFactory connectionFactory = new ConnectionFactory();

            // Only set these if specified by user. Otherwise, API will use default parameters.
            if (!string.IsNullOrEmpty(_connectionString))
            {
                connectionFactory.Uri = new Uri(_connectionString);
            }

            if (!string.IsNullOrEmpty(_hostName))
            {
                connectionFactory.HostName = _hostName;
            }

            if (!string.IsNullOrEmpty(_userName))
            {
                connectionFactory.UserName = _userName;
            }

            if (!string.IsNullOrEmpty(_password))
            {
                connectionFactory.Password = _password;
            }

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
