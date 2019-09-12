﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs
{
    [Binding]
    public sealed class RabbitMQTriggerAttribute : Attribute
    {

        public RabbitMQTriggerAttribute(string queueName)
        {
            QueueName = queueName;
        }

        public RabbitMQTriggerAttribute(string queueName, string deadLetterExchangeName)
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
        public string ConnectionStringSetting { get; set; }

        public string HostName { get; set; }

        public string QueueName { get; }

        [AppSetting]
        public string UserNameSetting { get; set; }

        [AppSetting]
        public string PasswordSetting { get; set; }

        public int Port { get; set; }

        public string DeadLetterExchangeName { get; }
    }
}
