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
        private readonly string _hostName;
        private readonly string _queueName;
        private readonly string _userName;
        private readonly string _password;
        private readonly int _port;
        private readonly bool _ssl;
        private readonly bool _insecureSsl;
        private readonly object _publishBatchLock;

        private IBasicPublishBatch _batch;

        public RabbitMQService(string connectionString, string hostName, string userName, string password, int port, bool ssl, bool insecureSsl)
        {
            _connectionString = connectionString;
            _hostName = hostName;
            _userName = userName;
            _password = password;
            _port = port;
            _ssl = ssl;
            _insecureSsl = insecureSsl;

            ConnectionFactory connectionFactory = GetConnectionFactory(_connectionString, _hostName, _userName, _password, _port, _ssl, _insecureSsl);

            _model = connectionFactory.CreateConnection().CreateModel();
            _publishBatchLock = new object();
        }

        public RabbitMQService(string connectionString, string hostName, string queueName, string userName, string password, int port, bool ssl, bool insecureSsl)
            : this(connectionString, hostName, userName, password, port, ssl, insecureSsl)
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

        internal static ConnectionFactory GetConnectionFactory(string connectionString, string hostName, string userName, string password, int port, bool ssl, bool insecureSsl)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory();

            // Only set these if specified by user. Otherwise, API will use default parameters.
            if (!string.IsNullOrEmpty(connectionString))
            {
                Uri amqpUri = new Uri(connectionString);
                connectionFactory.Uri = amqpUri;
                ConfigureSsl(connectionFactory, amqpUri.Host, ssl, insecureSsl);
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

                ConfigureSsl(connectionFactory, hostName, ssl, insecureSsl);

            }

            return connectionFactory;
        }

        internal static void ConfigureSsl(ConnectionFactory connectionFactory, string hostname, bool ssl, bool insecureSsl)
        {
            if (ssl)
            {
                connectionFactory.Ssl = new SslOption
                {
                    Enabled = true,

                    // Set SNI in order to work for multiple RabbitMQ clusters located behind a LoadBalancer
                    ServerName = hostname,
                };
                if (insecureSsl)
                {
                    connectionFactory.Ssl.AcceptablePolicyErrors =
                        SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors;
                }
            }
        }
    }
}
