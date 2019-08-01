using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class DefaultRabbitMQServiceFactory : IRabbitMQServiceFactory
    {
        public IRabbitMQService CreateService(string hostname)
        {
            return new RabbitMQService(hostname);
        }
    }
}
