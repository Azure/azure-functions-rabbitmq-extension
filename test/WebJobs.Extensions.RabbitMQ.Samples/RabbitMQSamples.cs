﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebJobs.Extensions.RabbitMQ.Samples
{
    public static class RabbitMQSamples
    {
        // Output samples
        // To run this sample with a specified amqp connection string, create a file called "appsettings.json" in the same directory.
        // In the file, add:
        // {
        //      "connectionStrings": {
        //          "rabbitMQ": "your connection string here"
        //      }
        // }
        // Or, if you already have an appsettings.json, add rabbitMQ and your connection string to the connection strings property.
        public static void TimerTrigger_ConnectionString_StringOutput(
            [TimerTrigger("00:01")] TimerInfo timer,
            [RabbitMQ(QueueName = "queue")] out string outputMessage,
            ILogger logger)
        {
            outputMessage = "new";
            logger.LogInformation($"RabbitMQ output binding message: {outputMessage}");
        }

        public static void TimerTrigger_PocoOutput(
             [TimerTrigger("00:01")] TimerInfo timer,
             [RabbitMQ(HostName = "localhost", QueueName = "queue")] out TestClass outputMessage,
             ILogger logger)
        {
            outputMessage = new TestClass(1, 1);
            logger.LogInformation($"RabbitMQ output binding message: {JsonConvert.SerializeObject(outputMessage)}");
        }

        // To run:
        // 1. Create Azure Storage Account and go to the homepage for that account
        // 2. Look for Queue service and click Queues on the sidebar
        // 3. Create a queue named "samples-rabbitmq-messages"
        // 4. Add a message to the queue
        // 5. Run this sample and you will see the queue trigger fired.
        // *Note that any time the queue isn't empty, the trigger will continue to fire.
        // So you can add items to the queue while the sample is running, and the trigger will be called until the queue is empty.
        public static async Task ProcessMessage_RabbitMQAsyncCollector(
            [QueueTrigger(@"samples-rabbitmq-messages")] string message,
            [RabbitMQ(QueueName = "queue")] IAsyncCollector<byte[]> messages,
            ILogger logger)
        {
            logger.LogInformation($"Received queue trigger");
            byte[] messageInBytes = Encoding.UTF8.GetBytes(message);
            await messages.AddAsync(messageInBytes);
        }

        // To run:
        // 1. Create Azure Storage Account and go to the homepage for that account
        // 2. Look for Queue service and click Queues on the sidebar
        // 3. Create a queue named "samples-rabbitmq-messages"
        // 4. Add a message to the queue in POCO format (i.e.: "{ "name": Katie }")
        // 5. Run this sample and you will see the queue trigger fired.
        // *Note that any time the queue isn't empty, the trigger will continue to fire.
        // So you can add items to the queue while the sample is running, and the trigger will be called until the queue is empty.
        public static void QueueTrigger_RabbitMQOutput(
            [QueueTrigger(@"samples-rabbitmq-messages")] TestClass message,
            [RabbitMQ(QueueName = "queue")] out TestClass outputMessage,
            ILogger logger)
        {
            outputMessage = message;
            logger.LogInformation($"RabbitMQ output binding message: {JsonConvert.SerializeObject(outputMessage)}");
        }

        // Example that binds to client
        public static void BindToClient(
            [TimerTrigger("01:00", RunOnStartup = true)] TimerInfo timer,
            [RabbitMQ(ConnectionStringSetting = "rabbitMQ")] IModel client,
            ILogger logger)
        {
            QueueDeclareOk queue = client.QueueDeclare("hello", false, false, false, null);
            logger.LogInformation("Opening connection and creating queue!");
        }

        // Trigger samples
        public static void RabbitMQTrigger_String(
             [RabbitMQTrigger("new_test_queue", ConnectionStringSetting = "rabbitMQ")] string message,
             string consumerTag,
             ILogger logger)
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {message} and consumer tag: {consumerTag}");
        }

        public static void RabbitMQTrigger_String_NoConnectionString(
             [RabbitMQTrigger(hostName: "RabbitMQHostName", userNameSetting: "%UserNameSetting%", passwordSetting: "%PasswordSetting%", port: 5672, queueName: "queue")] string message,
             string consumerTag,
             ILogger logger)
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {message} and consumer tag: {consumerTag}");
        }

        public static void RabbitMQTrigger_BasicDeliverEventArgs(
            [RabbitMQTrigger("queue")] BasicDeliverEventArgs args,
            ILogger logger)
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {Encoding.UTF8.GetString(args.Body)}");
        }

        // This sample should fail when running a console app that sends out a message incorrectly formatted.
        public static void RabbitMQTrigger_JsonToPOCO(
            [RabbitMQTrigger("new_test_queue", ConnectionStringSetting = "rabbitMQ")] TestClass pocObj,
            ILogger logger)
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {pocObj}");
        }

        // This sample waits on messages from the poison queue created by the above sample.
        // It should process it correctly since it's configured to be of type string.
        public static void RabbitMQTrigger_Process_PoisonQueue(
            [RabbitMQTrigger("new_test_queue-poison", ConnectionStringSetting = "rabbitMQ")] string res,
            ILogger logger)
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {res}");
        }

        public static void RabbitMQTrigger_RabbitMQOutput(
            [RabbitMQTrigger("queue")] string inputMessage,
            [RabbitMQ(
                HostName = "localhost",
                QueueName = "hello")] out string outputMessage,
            ILogger logger)
        {
            outputMessage = inputMessage;
            logger.LogInformation($"RabbitMQ output binding function sent message: {outputMessage}");
            logger.LogInformation($"RabbitMQ output binding function sent message: {outputMessage}");
        }

        public static void RabbitMQTrigger_String_NoConnectionString_WithVirtualHost(
            [RabbitMQTrigger(hostName: "%RabbitMQHostName%", userNameSetting: "%UserNameSetting%", passwordSetting: "%PasswordSetting%", port: 5672, queueName: "queue-in-vhost", VirtualHost = "azure-rabbitmq-vhost")] string message,
            string consumerTag,
            ILogger logger)
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {message} and consumer tag: {consumerTag}");
        }

        public class TestClass
        {
            private readonly int _x;
            private readonly int _y;

            public TestClass(int x, int y)
            {
                _x = x;
                _y = y;
            }
        }
    }
}
