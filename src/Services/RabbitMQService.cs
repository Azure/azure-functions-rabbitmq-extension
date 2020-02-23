// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        private IRabbitMQModel _rabbitMQModel;
        private IModel _model;
        private IBasicPublishBatch _batch;

        public IRabbitMQModel RabbitMQModel => _rabbitMQModel;

        public IModel Model => _model;

        public IBasicPublishBatch BasicPublishBatch => _batch;

        public RabbitMQService(string connectionString)
        {
            ConnectionFactory connectionFactory = GetConnectionFactory(connectionString);

            _model = connectionFactory.CreateConnection().CreateModel();
        }

        public RabbitMQService(string connectionString, QueueConfiguration queueConfig)
            : this(connectionString)
        {
            _rabbitMQModel = new RabbitMQModel(_model);

            var deadLetterExchangeName = queueConfig.DeadLetterExchangeName;
            var queueName = queueConfig.Name ?? throw new ArgumentNullException(nameof(queueConfig.Name));

            Dictionary<string, object> args = queueConfig.Arguments.ToDictionary(k => k.Key, k => k.Value);

            // Create dead letter queue
            if (!string.IsNullOrEmpty(deadLetterExchangeName))
            {
                string deadLetterQueueName = string.Format("{0}-poison", queueName);
                _model.QueueDeclare(queue: deadLetterQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                _model.ExchangeDeclare(deadLetterExchangeName, Constants.DefaultDLXSetting);
                _model.QueueBind(deadLetterQueueName, deadLetterExchangeName, Constants.DeadLetterRoutingKeyValue, null);

                args[Constants.DeadLetterExchangeKey] = deadLetterExchangeName;
                args[Constants.DeadLetterRoutingKey] = Constants.DeadLetterRoutingKeyValue;
            }

            _model.QueueDeclare(
                queueName,
                durable: queueConfig.Durable,
                exclusive: queueConfig.Exclusive,
                autoDelete: queueConfig.AutoDelete,
                arguments: args);
            _batch = _model.CreateBasicPublishBatch();
        }

        internal static ConnectionFactory GetConnectionFactory(string connectionString)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
                DispatchConsumersAsync = true,
            };

            return connectionFactory;
        }
    }
}
