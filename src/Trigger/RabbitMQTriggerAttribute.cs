// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs
{
    [Binding]
    public sealed class RabbitMQTriggerAttribute : Attribute
    {

        public RabbitMQTriggerAttribute(string queueName)
            : this(null, null, null, 0, queueName)
        {}

        public RabbitMQTriggerAttribute(string hostName, string userNameSetting, string passwordSetting, int port, string queueName, bool queueDurable = false)
        {
            HostName = hostName;
            UserNameSetting = userNameSetting;
            PasswordSetting = passwordSetting;
            Port = port;
            QueueName = queueName;
            QueueDurable = queueDurable;
        }

        [ConnectionString]
        public string ConnectionStringSetting { get; set; }

        public string HostName { get; set; }

        public string QueueName { get; }

        public bool QueueDurable { get; }

        [AppSetting]
        public string UserNameSetting { get; set; }

        [AppSetting]
        public string PasswordSetting { get; set; }

        public int Port { get; set; }

        public string DeadLetterExchangeName { get; set; }
    }
}
