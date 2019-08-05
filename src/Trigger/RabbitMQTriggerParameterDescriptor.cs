// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Trigger
{
    internal class RabbitMQTriggerParameterDescriptor : TriggerParameterDescriptor
    {
        public string Hostname { get; set; }

        public string QueueName { get; set; }
    }
}
