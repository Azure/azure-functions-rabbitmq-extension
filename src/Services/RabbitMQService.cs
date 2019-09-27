// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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
        private string _deadLetterExchangeName;

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

        public RabbitMQService(string connectionString, string hostName, string exchangeName, string xMatch, string arguments, string userName, string password, int port)
            : this(connectionString, hostName, userName, password, port)
        {
            var result = _model.QueueDeclare();
            _queueName = result.QueueName;

            var compiledArguments = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(xMatch))
            {
                compiledArguments.Add("x-match", xMatch);
            }

            if (!string.IsNullOrWhiteSpace(arguments))
            {
                var argumentObject = JObject.Parse(arguments);

                foreach (var argument in argumentObject)
                {
                    compiledArguments.Add(argument.Key, argument.Value.ToString());
                }
            }

            _model.QueueBind(result.QueueName, exchangeName, string.Empty, compiledArguments);
            _rabbitMQModel = new RabbitMQModel(_model);
        }

        public RabbitMQService(string connectionString, string hostName, string queueName, string userName, string password, int port, string deadLetterExchangeName)
            : this(connectionString, hostName, userName, password, port)
        {
            _rabbitMQModel = new RabbitMQModel(_model);

            _deadLetterExchangeName = deadLetterExchangeName;
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

            Dictionary<string, object> args = new Dictionary<string, object>();

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

            _model.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: args);
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
