// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class RabbitMQTriggerParameterDescriptor : TriggerParameterDescriptor
    {
        public string Hostname { get; set; }

        public string QueueName { get; set; }

        public override string GetTriggerReason(IDictionary<string, string> arguments)
        {
            return string.Format("RabbitMQ message detected from queue: {0} at {1}", QueueName, DateTime.Now);
        }
    }
}
