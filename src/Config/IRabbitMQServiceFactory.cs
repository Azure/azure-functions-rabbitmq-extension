using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal interface IRabbitMQServiceFactory
    {
        IRabbitMQService CreateService(string hostname);
    }
}
