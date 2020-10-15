// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Trigger;
using Microsoft.Extensions.Logging;

namespace WebJobs.Extensions.RabbitMQ.Samples
{
    public static class RabbitMQSamplesChainedErrorQueues
    {
        private const string DefaultExchangeName = "";

        // This sample show how to listen to the message that failed to be processed and ends up into Dead Letter Queue
        // after they have been retried
        public static void RabbitMQTrigger_ChainedQueue1(
            [RabbitMQTrigger(
                "chained-test-queue",
                ConnectionStringSetting = "rabbitMQ",
                QueueDefinitionFactoryType = typeof(ProcessorQueueDefinitionFactory))
            ]
            string message,
            ILogger logger
        )
        {
            logger.LogInformation($"Received message in main queue: {message}");
            throw new Exception("An error occured");
        }

        public static void RabbitMQTrigger_ChainedQueue2(
            [RabbitMQTrigger(
                "chained-test-queue-failed",
                ConnectionStringSetting = "rabbitMQ",
                QueueDefinitionFactoryType = typeof(FailedQueueDefinitionFactory))
            ]
            string message,
            ILogger logger
        )
        {
            logger.LogWarning($"An error occured while trying to process a message: {message}, let's handle error here (put something in error state for example)");
            throw new Exception("An error occured");
        }

        public class ProcessorQueueDefinitionFactory : IRabbitMQQueueDefinitionFactory
        {
            public RabbitMQQueueDefinition BuildDefinition(string queueName)
            {
                return new RabbitMQQueueDefinitionBuilder(QueueType.Classic)
                    .Durable()
                    .WithDeadLetterExchange(DefaultExchangeName)
                    .WithDeadLetterRoutingKey(this.GetDeadLetterQueueName(queueName))
                    .Build();
            }

            public string GetDeadLetterQueueName(string queueName)
            {
                return queueName + "-failed";
            }

            public RabbitMQQueueDefinition BuildDeadLetterQueueDefinition(string deadLetterQueueName)
            {
                return new RabbitMQQueueDefinitionBuilder(QueueType.Classic)
                    .Durable()
                    .WithDeadLetterExchange(DefaultExchangeName)
                    .WithDeadLetterRoutingKey(deadLetterQueueName.Substring(0, deadLetterQueueName.LastIndexOf('-')) + "-error")
                    .Build();
            }
        }

        public class FailedQueueDefinitionFactory : IRabbitMQQueueDefinitionFactory
        {
            public RabbitMQQueueDefinition BuildDefinition(string queueName)
            {
                return new RabbitMQQueueDefinitionBuilder(QueueType.Classic)
                    .Durable()
                    .WithDeadLetterExchange(DefaultExchangeName)
                    .WithDeadLetterRoutingKey(this.GetDeadLetterQueueName(queueName))
                    .Build();
            }

            public string GetDeadLetterQueueName(string queueName)
            {
                return queueName.Substring(0, queueName.LastIndexOf('-')) + "-error";
            }

            public RabbitMQQueueDefinition BuildDeadLetterQueueDefinition(string deadLetterQueueName)
            {
                return new RabbitMQQueueDefinitionBuilder(QueueType.Classic)
                    .Durable()
                    .WithMessageTtl(TimeSpan.FromSeconds(60))
                    .Build();
            }
        }
    }
}