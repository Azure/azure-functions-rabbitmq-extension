// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        private readonly IRabbitMQModel _rabbitMQModel;
        private readonly IModel _model;
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly bool _disableCertificateValidation;
        private readonly object _publishBatchLock;

        private IBasicPublishBatch _batch;

        public RabbitMQService(string connectionString, bool disableCertificateValidation)
        {
            _connectionString = connectionString;
            _disableCertificateValidation = disableCertificateValidation;

            ConnectionFactory connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
            };

            if (disableCertificateValidation && connectionFactory.Ssl.Enabled)
            {
                connectionFactory.Ssl.AcceptablePolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }

            _model = connectionFactory.CreateConnection().CreateModel();
            _publishBatchLock = new object();
        }

        public RabbitMQService(string connectionString, string queueName, bool disableCertificateValidation)
            : this(connectionString, disableCertificateValidation)
        {
            _rabbitMQModel = new RabbitMQModel(_model);
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

            _model.QueueDeclarePassive(_queueName); // Throws exception if queue doesn't exist
            _batch = _model.CreateBasicPublishBatch();
        }

        public IRabbitMQModel RabbitMQModel => _rabbitMQModel;

        public IModel Model => _model;

        public IBasicPublishBatch BasicPublishBatch => _batch;

        public object PublishBatchLock => _publishBatchLock;

        // Typically called after a flush
        public void ResetPublishBatch()
        {
            _batch = _model.CreateBasicPublishBatch();
        }
    }
}
