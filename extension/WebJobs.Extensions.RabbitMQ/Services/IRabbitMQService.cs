// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

public interface IRabbitMQService
{
    IBasicPublishBatch BasicPublishBatch { get; }

    object PublishBatchLock { get; }

    void ResetPublishBatch();

    void ConfigureQos(uint prefetchSize, ushort prefetchCount, bool global);

    QueueDeclareOk GetQueueInfo(string queueName);

    AsyncEventingBasicConsumer CreateConsumer();

    string Consume(string queue, bool autoAck, AsyncEventingBasicConsumer consumer);

    void OnMessageConsumed(string consumerTag, ulong deliveryTag);

    void Acknowledge(ulong deliveryTag, bool multiple, string logDetails);

    void Reject(ulong deliveryTag, bool requeue, string logDetails);

    void Publish(string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body);

    void Cancel(string consumerTag);

    void Close();
}
