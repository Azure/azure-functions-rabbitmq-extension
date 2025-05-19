// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal sealed class RabbitMQService : IRabbitMQService
{
    private readonly ILogger logger;

    public RabbitMQService(string connectionString, bool disableCertificateValidation, ILogger logger)
    {
        var connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),

            // Required to use async consumer. See: https://www.rabbitmq.com/dotnet-api-guide.html#consuming-async.
            DispatchConsumersAsync = true,
        };

        if (disableCertificateValidation && connectionFactory.Ssl.Enabled)
        {
            connectionFactory.Ssl.AcceptablePolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
        }

        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Model = this.AddShutdownHandler(connectionFactory.CreateConnection().CreateModel());
        this.PublishBatchLock = new object();
    }

    public RabbitMQService(string connectionString, string queueName, bool disableCertificateValidation, ILogger logger)
        : this(connectionString, disableCertificateValidation, logger)
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

    private IModel AddShutdownHandler(IModel model)
    {
        model.ModelShutdown += (sender, args) =>
        {
            this.logger.LogError($"[!] Channel closed due to error: {args.Exception?.Message}");
        };
        return model;
    }
}
