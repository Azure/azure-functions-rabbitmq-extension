// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Json;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebJobs.Extensions.RabbitMQ.Samples
{
    public static class RabbitMQTriggerSamples
    {
        public static void RabbitMQTrigger_String(
            [RabbitMQTrigger("localhost", "queue")] string message,
            string consumerTag,
            ILogger logger
        )
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message consumer tag: {consumerTag}");
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {message}");
        }

        public static void RabbitMQTrigger_BasicDeliverEventArgs(
            [RabbitMQTrigger("localhost", "queue")] BasicDeliverEventArgs args,
            ILogger logger
        )
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {Encoding.UTF8.GetString(args.Body)}");
        }

        public static void RabbitMQTrigger_Json(
            [RabbitMQTrigger("localhost", "queue")] JsonValue jsonObj,
            ILogger logger
        )
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {jsonObj}");
        }

        public static void RabbitMQTrigger_JsonToPOCO(
            [RabbitMQTrigger("localhost", "queue")] TestClass pocObj,
            ILogger logger
        )
        {
            logger.LogInformation($"RabbitMQ queue trigger function processed message: {pocObj}");

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
