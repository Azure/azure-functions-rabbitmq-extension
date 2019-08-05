// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    [Binding]
    public sealed class RabbitMQTriggerAttribute : Attribute
    {
        public RabbitMQTriggerAttribute(string hostname, string queueName)
        {
            this.Hostname = hostname;
            this.QueueName = queueName;
            this.BatchNumber = 1;
        }

        public RabbitMQTriggerAttribute(string hostname, string queueName, ushort batchNumber)
        {
            this.Hostname = hostname;
            this.QueueName = queueName;
            this.BatchNumber = batchNumber;
        }

        public string Hostname { get; }

        public string QueueName { get; }

        public ushort BatchNumber { get; }
    }
}
