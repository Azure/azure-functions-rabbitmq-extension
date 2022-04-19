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
            this.Model = model;
        }

        public IModel Model { get; }

        public IBasicPublishBatch CreateBasicPublishBatch()
        {
            return this.Model.CreateBasicPublishBatch();
        }

        public QueueDeclareOk QueueDeclarePassive(string queue)
        {
            return this.Model.QueueDeclarePassive(queue);
        }

        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            return this.Model.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            this.Model.QueueBind(queue, exchange, routingKey, arguments);
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            this.Model.BasicQos(prefetchSize, prefetchCount, global);
        }

        public string BasicConsume(string queue, bool autoAck, IBasicConsumer consumer)
        {
            return this.Model.BasicConsume(queue, autoAck, consumer);
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            this.Model.BasicAck(deliveryTag, multiple);
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            this.Model.BasicReject(deliveryTag, requeue);
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        {
            this.Model.BasicPublish(exchange, routingKey, basicProperties, body);
        }

        public void BasicCancel(string consumerTag)
        {
            this.Model.BasicCancel(consumerTag);
        }

        public void ExchangeDeclare(string exchange, string exchangeType)
        {
            this.Model.ExchangeDeclare(exchange, exchangeType);
        }

        public void Close()
        {
            this.Model.Close();
        }
    }
}
