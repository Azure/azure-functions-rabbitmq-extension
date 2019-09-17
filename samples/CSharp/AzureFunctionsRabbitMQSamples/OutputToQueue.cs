using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace AzureFunctionsRabbitMQSamples
{
    public class OutputToQueue
    {
        public OutputToQueue()
        {
        }

        [FunctionName("OutputToQueue")]
        [return : RabbitMQ(ConnectionStringSetting = "RabbitMQConnection",
            QueueName = "queue")]
        public async Task<byte[]> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = new StreamReader(req.Body).ReadToEndAsync().Result;
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            return await Task.FromResult(Encoding.ASCII.GetBytes(requestBody));
        }

        [FunctionName("OutputToQueue2")]
        public async Task<IActionResult> RunAsync2(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [RabbitMQ(ConnectionStringSetting = "RabbitMQConnection",
            QueueName = "queue2")] IAsyncCollector<byte[]> messages, 
            
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = new StreamReader(req.Body).ReadToEndAsync().Result;
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            byte[] messageContent = Encoding.ASCII.GetBytes(requestBody);
            await messages.AddAsync(messageContent);
            return new AcceptedResult();
        }
    }
}
