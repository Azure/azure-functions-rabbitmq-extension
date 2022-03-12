// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class RabbitMQContext
    {
        public RabbitMQAttribute ResolvedAttribute { get; set; }

        public IRabbitMQService Service { get; set; }
    }
}
