// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        private IRabbitMQModel _rabbitMQModel;
        private IModel _model;
        private IBasicPublishBatch _batch;
        private string _connectionString;
        private string _hostName;
        private string _queueName;
        private bool _queueDurable;
        private string _userName;
        private string _password;
        private int _port;
        private string _deadLetterExchangeName;

        public IRabbitMQModel RabbitMQModel => _rabbitMQModel;
        public IModel Model => _model;
        public IBasicPublishBatch BasicPublishBatch => _batch;

        public RabbitMQService(string connectionString, string hostName, string userName, string password, int port)
        {
            ConnectionFactory connectionFactory = GetConnectionFactory(connectionString, hostName, userName, password, port);

            _model = connectionFactory.CreateConnection().CreateModel();
        }

        public RabbitMQService(string connectionString, string hostName, string queueName, bool queueDurable, string userName, string password, int port, string deadLetterExchangeName)
            : this(connectionString, hostName, userName, password, port)
        {
            _rabbitMQModel = new RabbitMQModel(_model);
            _queueDurable = queueDurable;
            _deadLetterExchangeName = deadLetterExchangeName;

            string resolvedQueueName = queueName ?? string.Empty;

            Dictionary<string, object> args = new Dictionary<string, object>();

            QueueDeclareOk queueDeclareResult = _model.QueueDeclare(queue: resolvedQueueName, durable: _queueDurable, exclusive: false, autoDelete: false, arguments: args);
            _queueName = queueDeclareResult.QueueName;

            // Create dead letter queue
            if (!string.IsNullOrEmpty(_deadLetterExchangeName))
            {
                string deadLetterQueueName = string.Format("{0}-poison", _queueName);
                _model.QueueDeclare(queue: deadLetterQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                _model.ExchangeDeclare(_deadLetterExchangeName, Constants.DefaultDLXSetting);
                _model.QueueBind(deadLetterQueueName, _deadLetterExchangeName, Constants.DeadLetterRoutingKeyValue, null);

                args[Constants.DeadLetterExchangeKey] = _deadLetterExchangeName;
                args[Constants.DeadLetterRoutingKey] = Constants.DeadLetterRoutingKeyValue;
            }

            _batch = _model.CreateBasicPublishBatch();
        }

        internal static ConnectionFactory GetConnectionFactory(string connectionString, string hostName, string userName, string password, int port)
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

                if (port != 0)
                {
                    connectionFactory.Port = port;
                }
            }

            return connectionFactory;
        }
    }
}
