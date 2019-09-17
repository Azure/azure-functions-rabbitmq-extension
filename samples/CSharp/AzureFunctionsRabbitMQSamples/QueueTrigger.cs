using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionsRabbitMQSamples
{
    public class QueueTrigger
    {

        [FunctionName("QueueTrigger")]
        public void Run([RabbitMQTrigger("queue",
            ConnectionStringSetting = "RabbitMQConnection"
            
            )] string message,
        ILogger log)
        {
            log.LogInformation($"Message received from RabbitMQ trigger: {message}");
        }
    }
}
