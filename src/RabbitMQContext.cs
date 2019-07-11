// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions
{
    public class RabbitMQContext
    {
        public string Hostname { get; set; }

        public string QueueName { get; set; }

        public string Message { get; set; }
    }
}
