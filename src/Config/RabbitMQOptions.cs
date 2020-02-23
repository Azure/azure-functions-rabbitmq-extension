// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class RabbitMQOptions
    {
        public string HostName { get; set; }

        public string QueueName { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string ConnectionString { get; set; }

        public int Port { get; set; }

        public string DeadLetterExchangeName { get; set; }

        public RabbitMqTriggerOptions Trigger { get; set; }
    }
}