//// Copyright (c) .NET Foundation. All rights reserved.
//// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace WebJobs.Extensions.RabbitMQ.Samples
{
    public static class RabbitMQSamples
    {
        // Output samples
        public static void TimerTrigger_StringOutput(
            [TimerTrigger("00:01")] TimerInfo timer,
            [RabbitMQ(
                HostName = "localhost",
                QueueName = "queue")] out string outputMessage,
            ILogger logger)
        {
            outputMessage = "new";
            logger.LogInformation($"RabbitMQ output binding message: {outputMessage}");
        }

        public static void TimerTrigger_PocoOutput(
             [TimerTrigger("00:01")] TimerInfo timer,
             [RabbitMQ(
                  HostName = "localhost",
                  QueueName = "queue")] out TestClass outputMessage,
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
            [RabbitMQ(
                HostName = "localhost",
                QueueName = "queue")] IAsyncCollector<byte[]> messages,
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
            [RabbitMQ(
                HostName = "localhost",
                QueueName = "queue")] out TestClass outputMessage,
            ILogger logger)
        {
            outputMessage = message;
            logger.LogInformation($"RabbitMQ output binding message: {JsonConvert.SerializeObject(outputMessage)}");
        }

        // Trigger samples
        public static void RabbitMQTrigger_String(
             [RabbitMQTrigger("localhost", "queue")] string message,
             string consumerTag,
             ILogger logger)
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message consumer tag: {consumerTag}");
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {message}");
        }

        public static void RabbitMQTrigger_BasicDeliverEventArgs(
            [RabbitMQTrigger("localhost", "queue")] BasicDeliverEventArgs args,
            ILogger logger)
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {Encoding.UTF8.GetString(args.Body)}");
        }

        public static void RabbitMQTrigger_JsonToPOCO(
            [RabbitMQTrigger("localhost", "queue")] TestClass pocObj,
            ILogger logger)
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {pocObj}");
        }

        public static void RabbitMQTrigger_RabbitMQOutput(
            [RabbitMQTrigger("localhost", "queue")] string inputMessage,
            [RabbitMQ(
                HostName = "localhost",
                QueueName = "hello")] out string outputMessage,
            ILogger logger)
        {
            outputMessage = inputMessage;
            logger.LogInformation($"RabittMQ output binding function sent message: {outputMessage}");
        }

        public class TestClass
        {
            private readonly int x;
            private readonly int y;

            public TestClass(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
    }
}
