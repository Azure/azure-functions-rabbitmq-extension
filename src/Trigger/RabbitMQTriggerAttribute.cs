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
            this.ConnectionStringSetting = connectionString;
            this.QueueName = queueName;
        }

        //public RabbitMQTriggerAttribute(string hostName, string queueName)
        //{
        //    this.HostName = hostName;
        //    this.QueueName = queueName;
        //}

        public RabbitMQTriggerAttribute(string hostName, string queueName, ushort batchNumber)
        {
            this.HostName = hostName;
            this.QueueName = queueName;
            this.BatchNumber = batchNumber;
        }

        public RabbitMQTriggerAttribute(string hostName, string queueName, string userName, string password, int port)
        {
            this.HostName = hostName;
            this.QueueName = queueName;
            this.UserName = userName;
            this.Password = password;
            this.Port = port;
            this.BatchNumber = 1;
        }

        public RabbitMQTriggerAttribute(string connectionString, string hostName, string queueName, string userName, string password, int port)
        {
            this.ConnectionStringSetting = connectionString;
            this.HostName = hostName;
            this.QueueName = queueName;
            this.UserName = userName;
            this.Password = password;
            this.Port = port;
        }

        public string ConnectionStringSetting { get;  }

        public string HostName { get; }

        public string QueueName { get; }

        public string UserName { get; }

        public string Password { get; }

        public int Port { get; }

        public ushort BatchNumber { get; }
    }
}
