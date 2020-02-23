// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class TriggerConfiguration
    {
        private ushort _prefetchCount;

        public ushort PrefetchCount
        {
            get => _prefetchCount;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException($"{nameof(PrefetchCount)} value must be greater than 0");
                }

                _prefetchCount = value;
            }
        }

        public QueueConfiguration Queue { get; set; }
    }
}