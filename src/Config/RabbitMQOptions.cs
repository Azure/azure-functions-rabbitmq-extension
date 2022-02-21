// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
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
        /// Gets the RabbitMQ queue name.
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

        public string Format()
        {
            var options = new JObject
            {
                [nameof(QueueName)] = QueueName,
                [nameof(PrefetchCount)] = PrefetchCount,
                [nameof(DisableCertificateValidation)] = DisableCertificateValidation,
            };

            return options.ToString(Formatting.Indented);
        }
    }
}
