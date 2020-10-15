// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        private readonly IRabbitMQModel _rabbitMQModel;
        private readonly IModel _model;
        private readonly IBasicPublishBatch _batch;
        private readonly string _connectionString;
        private readonly string _hostName;
        private readonly string _queueName;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _virtualHost;
        private readonly int _port;

        public RabbitMQService(string connectionString, string hostName, string userName, string password, int port, string virtualHost)
        {
            _connectionString = connectionString;
            _hostName = hostName;
            _userName = userName;
            _password = password;
            _port = port;
            _virtualHost = virtualHost;

            ConnectionFactory connectionFactory = GetConnectionFactory(_connectionString, _hostName, _userName, _password, _port, _virtualHost);

            _model = connectionFactory.CreateConnection().CreateModel();
        }

        public RabbitMQService(string connectionString, string hostName, string queueName, string userName, string password, int port, string virtualHost)
            : this(connectionString, hostName, userName, password, port, virtualHost)
        {
            _rabbitMQModel = new RabbitMQModel(_model);
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

            _model.QueueDeclarePassive(_queueName); // Throws exception if queue doesn't exist
            _batch = _model.CreateBasicPublishBatch();
        }

        public IRabbitMQModel RabbitMQModel => _rabbitMQModel;

        public IModel Model => _model;

        public IBasicPublishBatch BasicPublishBatch => _batch;

        internal static ConnectionFactory GetConnectionFactory(string connectionString, string hostName, string userName, string password, int port, string virtualHost)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory();

            // Only set these if specified by user. Otherwise, API will use default parameters.
            if (!string.IsNullOrEmpty(connectionString))
            {
                connectionFactory.Uri = new Uri(connectionString);
            }
            else
            {
                if (!string.IsNullOrEmpty(hostName))
                {
                    connectionFactory.HostName = hostName;
                }

                if (!string.IsNullOrEmpty(userName))
                {
                    connectionFactory.UserName = userName;
                }

                if (!string.IsNullOrEmpty(password))
                {
                    connectionFactory.Password = password;
                }

                if (!string.IsNullOrEmpty(virtualHost))
                {
                    connectionFactory.VirtualHost = virtualHost;
                }

                if (port != 0)
                {
                    connectionFactory.Port = port;
                }
            }

            return connectionFactory;
        }
    }
}
