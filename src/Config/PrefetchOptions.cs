// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class PrefetchOptions
    {
        public ushort PrefetchSize { get; set; }

        public ushort PrefetchCount { get; set; }
    }
}
