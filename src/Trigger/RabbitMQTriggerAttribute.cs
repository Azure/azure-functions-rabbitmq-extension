// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs
{
    [Binding]
    public sealed class RabbitMQTriggerAttribute : Attribute
    {
        public RabbitMQTriggerAttribute(string connectionStringSetting, string queueName)
        {
            ConnectionStringSetting = connectionStringSetting;
            QueueName = queueName;
        }

        public RabbitMQTriggerAttribute(string queueName)
        {
            QueueName = queueName;
        }

        public RabbitMQTriggerAttribute(string hostName, string userNameSetting, string passwordSetting, int port, string queueName)
        {
            HostName = hostName;
            UserNameSetting = userNameSetting;
            PasswordSetting = passwordSetting;
            Port = port;
            QueueName = queueName;
        }

        [ConnectionString]
        public string ConnectionStringSetting { get;  }

        public string HostName { get; }

        public string QueueName { get; }

        [AppSetting]
        public string UserNameSetting { get; }

        [AppSetting]
        public string PasswordSetting { get; }

        public int Port { get; }
    }
}
