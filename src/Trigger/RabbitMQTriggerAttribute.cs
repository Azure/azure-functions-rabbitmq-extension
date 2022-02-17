// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Attribute used to bind a parameter to RabbitMQ trigger message.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public sealed class RabbitMQTriggerAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="queueName">RabbitMQ queue name.</param>
        public RabbitMQTriggerAttribute(string queueName)
        {
            QueueName = queueName;
        }

        /// <summary>
        /// Gets or sets the setting name for RabbitMQ connection URI.
        /// </summary>
        [ConnectionString]
        public string ConnectionStringSetting { get; set; }

        /// <summary>
        /// Gets the RabbitMQ queue name.
        /// </summary>
        public string QueueName { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether certificate validation should be disabled. Not recommended for
        /// production. Does not apply when SSL is disabled.
        /// </summary>
        public bool DisableCertificateValidation { get; set; }
    }
}
