// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Trigger;
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
        private string _userName;
        private string _password;
        private int _port;

        public IRabbitMQModel RabbitMQModel => _rabbitMQModel;

        public IModel Model => _model;

        public IBasicPublishBatch BasicPublishBatch => _batch;

        public RabbitMQService(string connectionString, string hostName, string userName, string password, int port)
        {
            _connectionString = connectionString;
            _hostName = hostName;
            _userName = userName;
            _password = password;
            _port = port;

            ConnectionFactory connectionFactory = GetConnectionFactory(_connectionString, _hostName, _userName, _password, _port);

            _model = connectionFactory.CreateConnection().CreateModel();
        }

        public RabbitMQService(
            string connectionString,
            string hostName,
            string queueName,
            string userName,
            string password,
            int port,
            string deadLetterExchangeName,
            IRabbitMQQueueDefinitionFactory queueDefinitionFactory)
            : this(connectionString, hostName, userName, password, port)
        {
            _rabbitMQModel = new RabbitMQModel(_model);

            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

            Dictionary<string, object> args = new Dictionary<string, object>();

            var deadLetterQueueName = queueDefinitionFactory?.GetDeadLetterQueueName(_queueName);
            if (deadLetterQueueName != null)
            {
                DeclareQueue(deadLetterQueueName, queueDefinitionFactory.BuildDeadLetterQueueDefinition(deadLetterQueueName));
            }

            if (!string.IsNullOrEmpty(deadLetterExchangeName))
            {
                deadLetterQueueName = string.Format("{0}-poison", _queueName);
                _model.QueueDeclare(queue: deadLetterQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                _model.ExchangeDeclare(deadLetterExchangeName, Constants.DefaultDLXSetting);
                _model.QueueBind(deadLetterQueueName, deadLetterExchangeName, Constants.DeadLetterRoutingKeyValue, null);

                args[Constants.DeadLetterExchangeKey] = deadLetterExchangeName;
                args[Constants.DeadLetterRoutingKey] = Constants.DeadLetterRoutingKeyValue;
            }

            if (queueDefinitionFactory != null)
            {
                DeclareQueue(_queueName, queueDefinitionFactory.BuildDefinition(_queueName));
            }
            else
            {
                _model.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: args);
            }

            _batch = _model.CreateBasicPublishBatch();
        }

        private void DeclareQueue(string queueName, RabbitMQQueueDefinition queueDefinition)
        {
            _model.QueueDeclare(queueName, queueDefinition.Durable, queueDefinition.Exclusive, queueDefinition.AutoDelete, queueDefinition.Arguments);
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
