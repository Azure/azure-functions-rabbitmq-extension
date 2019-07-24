// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings;
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
                QueueName = "queue")] out string outputMessage)
        {
            outputMessage = "Hello world";
        }

        public static void TimerTrigger_RabbitMQOutput(
             [TimerTrigger("00:01")] TimerInfo timer,
             [RabbitMQ(
                  Hostname = "localhost",
                  QueueName = "queue")] out TestClass outputMessage)
        {
            outputMessage = new TestClass(1, 1);
            Console.WriteLine(outputMessage);
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
            [QueueTrigger(@"samples-rabbitmq-messages")] OpenType.Poco message,
            [RabbitMQ(
                Hostname = "localhost",
                QueueName = "queue")] out OpenType.Poco outputMessage)
        {
            outputMessage = message;
        }

        public class TestClass
        {
            public int x, y;

            public TestClass(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
    }
}
