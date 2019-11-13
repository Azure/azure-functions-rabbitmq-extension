// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        public IRabbitMQModel RabbitMQModel { get; }
        public IModel Model { get; }
        public IBasicPublishBatch BasicPublishBatch { get; }

        public RabbitMQService(string connectionString, string hostName, string userName, string password, int port)
        {
            ConnectionFactory connectionFactory = GetConnectionFactory(connectionString, hostName, userName, password, port);

            Model = connectionFactory.CreateConnection().CreateModel();
        }

        public RabbitMQService(string connectionString, string hostName, string queueName, bool queueDurable, string userName, string password, int port, string deadLetterExchangeName)
            : this(connectionString, hostName, userName, password, port)
        {
            RabbitMQModel = new RabbitMQModel(Model);

            string resolvedQueueName = queueName ?? string.Empty;

            Dictionary<string, object> args = new Dictionary<string, object>();

            QueueDeclareOk queueDeclareResult = Model.QueueDeclare(queue: resolvedQueueName, durable: queueDurable, exclusive: false, autoDelete: false, arguments: args);

            // Create dead letter queue
            if (!string.IsNullOrEmpty(deadLetterExchangeName))
            {
                string deadLetterQueueName = string.Format("{0}-poison", queueDeclareResult.QueueName);
                Model.QueueDeclare(queue: deadLetterQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                Model.ExchangeDeclare(deadLetterExchangeName, Constants.DefaultDLXSetting);
                Model.QueueBind(deadLetterQueueName, deadLetterExchangeName, Constants.DeadLetterRoutingKeyValue, null);

                args[Constants.DeadLetterExchangeKey] = deadLetterExchangeName;
                args[Constants.DeadLetterRoutingKey] = Constants.DeadLetterRoutingKeyValue;
            }

            BasicPublishBatch = Model.CreateBasicPublishBatch();
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
