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
        
         public RabbitMQTriggerAttribute(string queueName, bool queueDurabe, bool deadLetterQueueDurable, string deadLetterQueuesuffix, string deadLetterExchangeType, string deadLetterRoutingKey, string deadLetterExchangeName)
        {
            QueueName = queueName;
            QueueDurabe = queueDurabe;
            DeadLetterQueueDurable = deadLetterQueueDurable;
            DeadLetterQueueSuffix = deadLetterQueuesuffix;
            DeadLetterExchangeType = deadLetterExchangeType;
            DeadLetterRoutingKeyValue = deadLetterRoutingKey;
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

        public string DeadLetterExchangeName { get; set; }

        public bool QueueDurabe { get; set; }

        public bool DeadLetterQueueDurable { get; set; }

        public string DeadLetterQueueSuffix { get; set; }

        public string DeadLetterExchangeType { get; set; }

        public string DeadLetterRoutingKeyValue { get; set; }
    }
}
