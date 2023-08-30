// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal sealed class RabbitMQService : IRabbitMQService
{
    public RabbitMQService(string connectionString, bool disableCertificateValidation, string sslCertPath = null, string sslCertPassphrase = null, string sslCertThumbprint = null)
    {
        var connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),

            // Required to use async consumer. See: https://www.rabbitmq.com/dotnet-api-guide.html#consuming-async.
            DispatchConsumersAsync = true,
        };

        if (connectionFactory.Ssl.Enabled)
        {
            if (disableCertificateValidation)
            {
                connectionFactory.Ssl.AcceptablePolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }

            if (!string.IsNullOrEmpty(sslCertThumbprint))
            {
                connectionFactory.Ssl.Certs = GetCertsFromThumbprint(sslCertThumbprint);
            }

            if (!string.IsNullOrEmpty(sslCertPath))
            {
                connectionFactory.Ssl.CertPath = sslCertPath;
                connectionFactory.Ssl.CertPassphrase = sslCertPassphrase;
            }
        }

        this.Model = connectionFactory.CreateConnection().CreateModel();
        this.PublishBatchLock = new object();
    }

    public RabbitMQService(string connectionString, string queueName, bool disableCertificateValidation, string sslCertPath, string sslCertPassphrase, string sslCertThumbprint)
        : this(connectionString, disableCertificateValidation, sslCertPath, sslCertPassphrase, sslCertThumbprint)
    {
        _ = queueName ?? throw new ArgumentNullException(nameof(queueName));

        this.Model.QueueDeclarePassive(queueName); // Throws exception if queue doesn't exist
        this.BasicPublishBatch = this.Model.CreateBasicPublishBatch();
    }

    public IModel Model { get; }

    public IBasicPublishBatch BasicPublishBatch { get; private set; }

    public object PublishBatchLock { get; }

    // Typically called after a flush
    public void ResetPublishBatch()
    {
        this.BasicPublishBatch = this.Model.CreateBasicPublishBatch();
    }

    private static X509Certificate2Collection GetCertsFromThumbprint(string thumbprint)
    {
        using var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);

        certStore.Open(OpenFlags.ReadOnly);

        X509Certificate2Collection certCollection = certStore.Certificates.Find(
                                    X509FindType.FindByThumbprint,
                                    thumbprint,
                                    false);

        return certCollection.Count == 0
            ? throw new ArgumentException($"Certificate with thumbprint {thumbprint} was not found")
            : certCollection;
    }
}
