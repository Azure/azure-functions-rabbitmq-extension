// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        private readonly string _queueName;

        public RabbitMQService(string connectionString, bool disableCertificateValidation)
        {
            var connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
            };

            if (disableCertificateValidation && connectionFactory.Ssl.Enabled)
            {
                connectionFactory.Ssl.AcceptablePolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }

            Model = connectionFactory.CreateConnection().CreateModel();
            PublishBatchLock = new object();
        }

        public RabbitMQService(string connectionString, string queueName, bool disableCertificateValidation)
            : this(connectionString, disableCertificateValidation)
        {
            RabbitMQModel = new RabbitMQModel(Model);
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

            Model.QueueDeclarePassive(_queueName); // Throws exception if queue doesn't exist
            BasicPublishBatch = Model.CreateBasicPublishBatch();
        }

        public IRabbitMQModel RabbitMQModel { get; }

        public IModel Model { get; }

        public IBasicPublishBatch BasicPublishBatch { get; private set; }

        public object PublishBatchLock { get; }

        // Typically called after a flush
        public void ResetPublishBatch()
        {
            BasicPublishBatch = Model.CreateBasicPublishBatch();
        }
    }
}
