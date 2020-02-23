// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class RabbitMqTriggerOptions
    {
        public ushort PrefetchCount { get; set; }

        public bool IsDurableQueue { get; set; }

        public bool IsExclusiveQueue { get; set; }

        public bool IsAutoDeleteQueue { get; set; }

        public bool IsLazyQueue { get; set; }
    }
}