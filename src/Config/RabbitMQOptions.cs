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
        public RabbitMQOptions()
        {
            PrefetchCount = 30;
        }

        /// <summary>
        /// Gets or sets the HostName used to authenticate with RabbitMQ.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the QueueName to receive messages from or enqueue messages to.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Gets or sets the UserName used to authenticate with RabbitMQ.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the Password used to authenticate with RabbitMQ.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the ConnectionString used to authenticate with RabbitMQ.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the Port used. Defaults to 0.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the prefetch count while creating the RabbitMQ QoS. This seting controls how many values are cached.
        /// </summary>
        public ushort PrefetchCount { get; set; }

        public string Format()
        {
            JObject options = new JObject
            {
                { nameof(HostName), HostName },
                { nameof(QueueName), QueueName },
                { nameof(Port), Port },
                { nameof(PrefetchCount), PrefetchCount },
            };

            return options.ToString(Formatting.Indented);
        }
    }
}
