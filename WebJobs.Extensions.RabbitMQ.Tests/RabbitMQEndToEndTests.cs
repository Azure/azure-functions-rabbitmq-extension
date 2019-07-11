//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Linq;
//using System.Threading.Tasks;
//using WebJobs.Extensions.RabbitMQ.EndToEnd;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Azure.WebJobs.Extensions.Tests.Common;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using RabbitMQ.Client;
//using RabbitMQ.Client.Events;
//using Xunit;

//namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests
//{
//    public class RabbitMQEndToEndTests
//    {        
//        private const string HostName = "localhost";
//        private const string QueueName = "queue";
//        private const string Message = "Hello";
//        private readonly TestLoggerProvider _loggerProvider = new TestLoggerProvider();

//        [Fact]
//        public async Task RabbitMQEndToEnd()
//        {
//            using (var host = await StartHostAsync(typeof(EndToEndTestClass)))
//            {
//                var parameter = new Dictionary<string, string>();
//                string queueInput = "hello";
//                parameter["message"] = queueInput;

//                await host.GetJobHost().CallAsync(nameof(EndToEndTestClass.QueueTrigger_RabbitMQOutput), parameter);


//                // waits until trigger is called logging message
//                await TestHelpers.Await(() => 
//                { 
//                    return _loggerProvider.GetAllLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains("Trigger called")) == 4;
//                });

//                // Add logic for the receiver here
//                var factory = new ConnectionFactory() { HostName = HostName };
//                using (var connection = factory.CreateConnection())
//                using (var channel = connection.CreateModel())
//                {
//                    channel.QueueDeclare(queue: QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

//                    var consumer = new EventingBasicConsumer(channel);
//                    var receivedMessage = string.Empty;
//                    consumer.Received += (model, ea) =>
//                    {
//                        var body = ea.Body;
//                        receivedMessage = Encoding.UTF8.GetString(body);
//                        Console.WriteLine("Received {0}", receivedMessage);
//                    };

//                    channel.BasicConsume(queue: QueueName, autoAck: true, consumer: consumer);

//                    Assert.Equal(receivedMessage, Message);
//                }
//            }
//        }

//        private async Task<IHost> StartHostAsync(Type testType)
//        {
//            ExplicitTypeLocator locator = new ExplicitTypeLocator(testType);

//            IHost host = new HostBuilder()
//                .ConfigureWebJobs(builder =>
//                {
//                    builder
//                    .AddAzureStorage()
//                    .AddRabbitMQ();
//                })
//                .ConfigureServices(services =>
//                {
//                    services.AddSingleton<ITypeLocator>(locator);

//                })
//                .ConfigureLogging(logging =>
//                {
//                    logging.SetMinimumLevel(LogLevel.Debug);
//                })
//                .Build();

//            await host.StartAsync();
//            return host;
//        }

//        private static class EndToEndTestClass
//        {
//            [NoAutomaticTrigger]
//            public static void QueueTrigger_RabbitMQOutput(
//           [QueueTrigger("NotUsed")] string message,
//           [RabbitMQ(
//                Hostname = "localhost",
//                QueueName = "queue",
//                Message = "{QueueTrigger}"
//            )] out string outputMessage,
//           ILogger log)
//            {
//                outputMessage = message;
//                log.LogInformation("Trigger called!");
//            }
//        }
//    }
//}
