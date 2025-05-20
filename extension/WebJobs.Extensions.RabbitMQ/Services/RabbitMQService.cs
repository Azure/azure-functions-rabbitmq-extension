// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Net.Security;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal sealed class RabbitMQService : IRabbitMQService
{
    private readonly ILogger logger;
    private readonly IModel model;
    private readonly ConcurrentDictionary<ulong, byte> deliveredTags = new();

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
        this.model = this.AddShutdownHandler(connectionFactory.CreateConnection().CreateModel());
        this.PublishBatchLock = new object();
    }

    public RabbitMQService(string connectionString, string queueName, bool disableCertificateValidation, ILogger logger)
        : this(connectionString, disableCertificateValidation, logger)
    {
        _ = queueName ?? throw new ArgumentNullException(nameof(queueName));

        this.model.QueueDeclarePassive(queueName); // Throws exception if queue doesn't exist
        this.BasicPublishBatch = this.model.CreateBasicPublishBatch();
    }

    public IBasicPublishBatch BasicPublishBatch { get; private set; }

    public object PublishBatchLock { get; }

    // Typically called after a flush
    public void ResetPublishBatch()
    {
        this.BasicPublishBatch = this.model.CreateBasicPublishBatch();
    }

    public void ConfigureQos(uint prefetchSize, ushort prefetchCount, bool global)
    {
        this.model.BasicQos(prefetchSize, prefetchCount, global);
    }

    public QueueDeclareOk GetQueueInfo(string queueName)
    {
        return this.model.QueueDeclarePassive(queueName);
    }

    public AsyncEventingBasicConsumer CreateConsumer()
    {
        return new AsyncEventingBasicConsumer(this.model);
    }

    public string Consume(string queue, bool autoAck, AsyncEventingBasicConsumer consumer)
    {
        return this.model.BasicConsume(queue, autoAck, consumer);
    }

    public void OnMessageConsumed(string consumerTag, ulong deliveryTag)
    {
        this.deliveredTags.TryAdd(deliveryTag, 0);
    }

    public void Acknowledge(ulong deliveryTag, bool multiple, string logDetails, bool throwOnMissing = false)
    {
        if (this.deliveredTags.TryRemove(deliveryTag, out _))
        {
            this.model.BasicAck(deliveryTag, multiple);
        }
        else
        {
            string message = $"Failed to acknowledge the message. deliveryTag ({deliveryTag}) not found.";
            if (throwOnMissing)
            {
                throw new InvalidOperationException(message);
            }
            else
            {
                this.logger.LogError($"{message} for {logDetails}");
            }
        }
    }

    public void Reject(ulong deliveryTag, bool requeue, string logDetails, bool throwOnMissing = false)
    {
        if (this.deliveredTags.TryRemove(deliveryTag, out _))
        {
            this.model.BasicReject(deliveryTag, requeue);
        }
        else
        {
            string message = $"Failed to acknowledge the message. deliveryTag ({deliveryTag}) not found.";
            if (throwOnMissing)
            {
                throw new InvalidOperationException(message);
            }
            else
            {
                this.logger.LogError(message);
            }
        }
    }

    public BasicGetResult Get(string queue, bool autoAck)
    {
        return this.model.BasicGet(queue, autoAck);
    }

    public void Publish(string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
    {
        this.model.BasicPublish(exchange, routingKey, mandatory: false, basicProperties, body);
    }

    public void Cancel(string consumerTag)
    {
        this.model.BasicCancel(consumerTag);
    }

    public void Close()
    {
        this.model.Close();
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
