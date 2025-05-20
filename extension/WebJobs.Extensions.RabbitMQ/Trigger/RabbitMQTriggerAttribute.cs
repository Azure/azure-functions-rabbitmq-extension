// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs;

/// <summary>
/// Attribute used to bind a parameter to RabbitMQ trigger message.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RabbitMQTriggerAttribute"/> class.
/// </remarks>
/// <param name="queueName">RabbitMQ queue name.</param>
[AttributeUsage(AttributeTargets.Parameter)]
[Binding]
public sealed class RabbitMQTriggerAttribute(string queueName) : Attribute
{
    /// <summary>
    /// Gets or sets the setting name for RabbitMQ connection URI.
    /// </summary>
    [ConnectionString]
    public string ConnectionStringSetting { get; set; }

    /// <summary>
    /// Gets the RabbitMQ queue name.
    /// </summary>
    public string QueueName { get; private set; } = queueName;

    /// <summary>
    /// Gets or sets a value indicating whether certificate validation should be disabled. Not recommended for
    /// production. Does not apply when SSL is disabled.
    /// </summary>
    public bool DisableCertificateValidation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether message acknowledgements from service would be disabled and needs to be done manually.
    /// </summary>
    public bool DisableAck { get; set; }
}
