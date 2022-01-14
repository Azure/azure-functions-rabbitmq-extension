// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public interface IRabbitMQModel
    {
        IModel Model { get; }

        IBasicPublishBatch CreateBasicPublishBatch();

        QueueDeclareOk QueueDeclarePassive(string queue);

        QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments);

        void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments);

        void BasicQos(uint prefetchSize, ushort prefetchCount, bool global);

        string BasicConsume(string queue, bool autoAck, IBasicConsumer consumer);

        void BasicAck(ulong deliveryTag, bool multiple);

        void BasicReject(ulong deliveryTag, bool requeue);

        void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body);

        void BasicCancel(string consumerTag);

        void ExchangeDeclare(string exchange, string exchangeType);

        void Close();
    }
}
