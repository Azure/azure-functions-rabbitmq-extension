// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class RabbitMQModel : IRabbitMQModel
    {
        public RabbitMQModel(IModel model)
        {
            Model = model;
        }

        public IModel Model { get; }

        public IBasicPublishBatch CreateBasicPublishBatch()
        {
            return Model.CreateBasicPublishBatch();
        }

        public QueueDeclareOk QueueDeclarePassive(string queue)
        {
            return Model.QueueDeclarePassive(queue);
        }

        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            return Model.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            Model.QueueBind(queue, exchange, routingKey, arguments);
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            Model.BasicQos(prefetchSize, prefetchCount, global);
        }

        public string BasicConsume(string queue, bool autoAck, IBasicConsumer consumer)
        {
            return Model.BasicConsume(queue, autoAck, consumer);
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            Model.BasicAck(deliveryTag, multiple);
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            Model.BasicReject(deliveryTag, requeue);
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        {
            Model.BasicPublish(exchange, routingKey, basicProperties, body);
        }

        public void BasicCancel(string consumerTag)
        {
            Model.BasicCancel(consumerTag);
        }

        public void ExchangeDeclare(string exchange, string exchangeType)
        {
            Model.ExchangeDeclare(exchange, exchangeType);
        }

        public void Close()
        {
            Model.Close();
        }
    }
}
