// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

namespace Microsoft.Azure.WebJobs.Extensions
{
    internal class RabbitMQContext
    {
        public RabbitMQAttribute ResolvedAttribute { get; set; }

        public IRabbitMQService Service { get; set; }
    }
}
