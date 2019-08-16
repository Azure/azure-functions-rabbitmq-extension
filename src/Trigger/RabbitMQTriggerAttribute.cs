// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs
{
    [Binding]
    public sealed class RabbitMQTriggerAttribute : Attribute
    {
        public RabbitMQTriggerAttribute(string connectionString, string queueName)
        {
            ConnectionStringSetting = connectionString;
            QueueName = queueName;
        }

        public RabbitMQTriggerAttribute(string queueName)
        {
            QueueName = queueName;
        }

        public RabbitMQTriggerAttribute(string hostName, string userName, string password, int port, string queueName)
        {
            HostName = hostName;
            UserName = userName;
            Password = password;
            Port = port;
            QueueName = queueName;
        }

        [ConnectionString]
        public string ConnectionStringSetting { get;  }

        public string HostName { get; }

        public string QueueName { get; }

        [AppSetting]
        public string UserName { get; }

        [AppSetting]
        public string Password { get; }

        public int Port { get; }
    }
}
