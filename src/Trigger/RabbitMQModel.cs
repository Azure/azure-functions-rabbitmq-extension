// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class RabbitMQModel : IRabbitMQModel
    {
        private readonly IModel _model;

        public RabbitMQModel(IModel model)
        {
            _model = model;
        }

        public IModel Model => _model;

        public IBasicPublishBatch CreateBasicPublishBatch()
        {
            return _model.CreateBasicPublishBatch();
        }

        public QueueDeclareOk QueueDeclarePassive(string queue)
        {
            return _model.QueueDeclarePassive(queue);
        }

        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            return _model.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> args)
        {
            _model.QueueBind(queue, exchange, routingKey, args);
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            _model.BasicQos(prefetchSize, prefetchCount, global);
        }

        public string BasicConsume(string queue, bool autoAck, IBasicConsumer consumer)
        {
            return _model.BasicConsume(queue, autoAck, consumer);
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            _model.BasicAck(deliveryTag, multiple);
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            _model.BasicReject(deliveryTag, requeue);
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            _model.BasicPublish(exchange, routingKey, basicProperties, body);
        }

        public void BasicCancel(string consumerTag)
        {
            _model.BasicCancel(consumerTag);
        }

        public void ExchangeDeclare(string exchange, string exchangeType)
        {
            _model.ExchangeDeclare(exchange, exchangeType);
        }

        public void Close()
        {
            _model.Close();
        }
    }
}
