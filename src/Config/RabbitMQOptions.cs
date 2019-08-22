// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Description;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class RabbitMQOptions
    {
        public string HostName { get; set; }

        public string QueueName { get; set; }

        [AppSetting]
        public string UserName { get; set; }

        [AppSetting]
        public string Password { get; set; }

        [ConnectionString]
        public string ConnectionString { get; set; }

        public int Port { get; set; }
    }
}
