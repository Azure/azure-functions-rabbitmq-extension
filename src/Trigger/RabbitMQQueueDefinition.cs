#nullable enable
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Trigger
{
    public class RabbitMQQueueDefinition
    {
        public bool Durable { get; set; }

        public bool AutoDelete { get; set; }

        public bool Exclusive { get; set; }

        public IDictionary<string, object>? Arguments { get; set; }
    }
}