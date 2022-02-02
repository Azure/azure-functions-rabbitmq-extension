// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Xunit;
using Constants = Microsoft.Azure.WebJobs.Extensions.Constants;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQAsyncCollectorTests
    {
        [Fact]
        public async Task AddAsync_AddsMessagesToQueue()
        {
            var batchLock = new object();
            var mockRabbitMQService = new Mock<IRabbitMQService>(MockBehavior.Strict);
            var mockBatch = new Mock<IBasicPublishBatch>();
            mockRabbitMQService.Setup(m => m.BasicPublishBatch).Returns(mockBatch.Object);
            mockRabbitMQService.Setup(m => m.PublishBatchLock).Returns(batchLock);

            var attribute = new RabbitMQAttribute
            {
                HostName = Constants.LocalHost,
                QueueName = "queue",
            };

            var context = new RabbitMQContext
            {
                ResolvedAttribute = attribute,
                Service = mockRabbitMQService.Object
            };

            ILoggerFactory loggerFactory = new LoggerFactory();
            ILogger logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory(Constants.RabbitMQ));
            var collector = new RabbitMQAsyncCollector(context, logger);

            byte[] body = Encoding.UTF8.GetBytes("hi");
            await collector.AddAsync(body);

#pragma warning disable 618
            mockBatch.Verify(m => m.Add(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), body), Times.Exactly(1));
#pragma warning restore 618
        }
    }
}
