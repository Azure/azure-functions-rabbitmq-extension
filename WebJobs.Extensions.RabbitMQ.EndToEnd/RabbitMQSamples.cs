// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebJobs.Extensions.RabbitMQ.Samples
{
    public static class RabbitMQSamples
    {
        public static void TimerTrigger_RabbitMQOutput(
           [TimerTrigger("00:01")] TimerInfo timer,
           [RabbitMQ(
                Hostname = "localhost",
                QueueName = "queue",
                Message = "Hello there")] out string outputMessage)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                var receivedMessage = string.Empty;
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    receivedMessage = Encoding.UTF8.GetString(body);
                    Console.WriteLine("Received {0}", receivedMessage);
                };

                channel.BasicConsume(queue: "queue", autoAck: true, consumer: consumer);

                outputMessage = receivedMessage;
                Console.WriteLine(outputMessage);
            }
        }

        public static void QueueTrigger_RabbitMQOutput(
           [QueueTrigger(@"samples-rabbitmq-messages")] string message,
           [RabbitMQ(
                Hostname = "localhost",
                QueueName = "queue",
                Message = "{QueueTrigger}")] out string outputMessage)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                var receivedMessage = string.Empty;
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    receivedMessage = Encoding.UTF8.GetString(body);
                    Console.WriteLine("Received {0}", receivedMessage);
                };

                channel.BasicConsume(queue: "queue", autoAck: true, consumer: consumer);
                outputMessage = receivedMessage;
                Console.WriteLine(outputMessage);
            }
        }
    }
}
