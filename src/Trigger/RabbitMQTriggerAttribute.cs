// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs
{
    [Binding]
    public sealed class RabbitMQTriggerAttribute : Attribute
    {
        public RabbitMQTriggerAttribute(string hostName, string queueName)
        {
            this.HostName = hostName;
            this.QueueName = queueName;
            this.BatchNumber = 1;
        }

        public RabbitMQTriggerAttribute(string hostName, string queueName, ushort batchNumber)
        {
            this.HostName = hostName;
            this.QueueName = queueName;
            this.BatchNumber = batchNumber;
        }

        public string HostName { get; }

        public string QueueName { get; }

        public ushort BatchNumber { get; }
    }
}
