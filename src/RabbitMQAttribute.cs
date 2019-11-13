// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Attribute used to bind a parameter to a RabbitMQMessage that will automatically be
    /// sent when the function completes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]

    public sealed class RabbitMQAttribute : Attribute
    {
        // <summary>
        // Necessary for creating connections
        // </summary>
        [AutoResolve]
        public string HostName { get; set; }

        /// <summary>
        /// The name of the queue to connect to.
        /// </summary>
        /// <remarks>
        /// If <see langword="null"/> or <see cref="string.Empty"/> the broker generates a unique queue name on behalf of the application.
        /// Queue names starting with "amq." are reserved for internal use by the broker.
        /// Attempts to declare a queue with a name that violates this rule will result in a channel-level exception with reply code 403.
        /// </remarks>
        [AutoResolve]
        public string QueueName { get; set; }

        // Optional
        public bool QueueDurable { get; set; }

        [AppSetting]
        public string UserName { get; set; }

        [AppSetting]
        public string Password { get; set; }

        // Optional
        public int Port { get; set; }

        [ConnectionString]
        public string ConnectionStringSetting { get; set; }

        [AutoResolve]
        public string DeadLetterExchangeName { get; set; }
    }
}
