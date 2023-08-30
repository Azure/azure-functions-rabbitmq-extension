// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs;

/// <summary>
/// Attribute used to bind a parameter to RabbitMQ trigger message.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[Binding]
public sealed class RabbitMQTriggerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQTriggerAttribute"/> class.
    /// </summary>
    /// <param name="queueName">RabbitMQ queue name.</param>
    public RabbitMQTriggerAttribute(string queueName)
    {
        this.QueueName = queueName;
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

    /// <summary>
    /// Gets or sets the path to the client certificate to be used when connecting. Does not apply when SSL is disabled.
    /// </summary>
    public string SslCertPath { get; set; }

    /// <summary>
    /// Gets or sets the passphrase for the client certificate. Does not apply when SslCertPath isn't set.
    /// </summary>
    public string SslCertPassphrase { get; set; }

    /// <summary>
    /// Gets or sets the thumbprint of the client certificate stored in the Windows certificate store in Current User\My to be used when connecting.
    /// This is where certificates loaded into an app service are stored. See https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code
    /// Does not apply when SSL is disabled.
    /// </summary>
    public string SslCertThumbprint { get; set; }
}
