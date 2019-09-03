// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs
{
    [Binding]
    public sealed class RabbitMQTriggerAttribute : Attribute
    {
        public RabbitMQTriggerAttribute(string connectionStringSetting, string queueName, string deadLetterExchangeName = "")
        {
            ConnectionStringSetting = connectionStringSetting;
            QueueName = queueName;
            DeadLetterExchangeName = deadLetterExchangeName;
        }


        public RabbitMQTriggerAttribute(string queueName, string deadLetterExchangeName = "")
        {
            QueueName = queueName;
            DeadLetterExchangeName = deadLetterExchangeName;
        }

        public RabbitMQTriggerAttribute(string hostName, string userNameSetting, string passwordSetting, int port, string queueName, string deadLetterExchangeName = "")
        {
            HostName = hostName;
            UserNameSetting = userNameSetting;
            PasswordSetting = passwordSetting;
            Port = port;
            QueueName = queueName;
            DeadLetterExchangeName = deadLetterExchangeName;
        }

        [ConnectionString]
        public string ConnectionStringSetting { get; }

        public string HostName { get; }

        public string QueueName { get; }

        [AppSetting]
        public string UserNameSetting { get; }

        [AppSetting]
        public string PasswordSetting { get; }

        public int Port { get; }

        public string DeadLetterExchangeName { get; }
    }
}
