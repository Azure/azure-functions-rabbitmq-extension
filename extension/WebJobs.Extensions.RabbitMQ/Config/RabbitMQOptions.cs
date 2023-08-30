// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

/// <summary>
/// Configuration options for the RabbitMQ extension.
/// </summary>
public class RabbitMQOptions : IOptionsFormatter
{
    /// <summary>
    /// Gets or sets the RabbitMQ connection URI.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the RabbitMQ queue name.
    /// </summary>
    public string QueueName { get; set; }

    /// <summary>
    /// Gets or sets the RabbitMQ QoS prefetch-count setting. It controls the number of RabbitMQ messages cached.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 30;

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

    public string Format()
    {
        var options = new JObject
        {
            [nameof(this.QueueName)] = this.QueueName,
            [nameof(this.PrefetchCount)] = this.PrefetchCount,
            [nameof(this.DisableCertificateValidation)] = this.DisableCertificateValidation,
            [nameof(this.SslCertPath)] = this.SslCertPath,
            [nameof(this.SslCertPassphrase)] = this.SslCertPassphrase,
            [nameof(this.SslCertThumbprint)] = this.SslCertThumbprint,
        };

        return options.ToString(Formatting.Indented);
    }
}
