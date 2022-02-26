// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Attribute used to bind a parameter to RabbitMQ output message.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public sealed class RabbitMQAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the setting name for RabbitMQ connection URI.
        /// </summary>
        [ConnectionString]
        public string ConnectionStringSetting { get; set; }

        /// <summary>
        /// Gets or sets the RabbitMQ queue name.
        /// </summary>
        [AutoResolve]
        public string QueueName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether certificate validation should be disabled. Not recommended for
        /// production. Does not apply when SSL is disabled.
        /// </summary>
        public bool DisableCertificateValidation { get; set; }
    }
}
