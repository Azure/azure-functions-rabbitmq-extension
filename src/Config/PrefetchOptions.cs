// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    /// <summary>
    /// Configuration options for the RabbitMQ prefetch.
    /// </summary>
    public class PrefetchOptions
    {
        public PrefetchOptions()
        {
            PrefetchSize = 0;
            PrefetchCount = 30;
        }

        /// <summary>
        /// Gets or sets the PrefetchSize to be set while creating RabbitMQ client. Default value is 0. Please refer to https://www.rabbitmq.com/amqp-0-9-1-reference.html for more details about this property.
        /// </summary>
        public uint PrefetchSize { get; set; }

        /// <summary>
        /// Gets or sets the PrefetchCount to be set while creating RabbitMQ client. Default value is 30. Please refer to https://www.rabbitmq.com/amqp-0-9-1-reference.html for more details about this property.
        /// </summary>
        public ushort PrefetchCount { get; set; }
    }
}
