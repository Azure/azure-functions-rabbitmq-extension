// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQConfigurationTests
    {
        private static readonly IConfiguration _emptyConfig = new ConfigurationBuilder().Build();

        [Fact]
        public void Creates_Context_Correctly()
        {
            var options = new RabbitMQOptions { Hostname = "localhost", QueueName = "hello" };
            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(options), mockServiceFactory.Object, (ILoggerFactory)loggerFactory);
            var attribute = new RabbitMQAttribute { Hostname = "localhost", QueueName = "queue" };

            var actualContext = config.CreateContext(attribute);

            RabbitMQAttribute attr = new RabbitMQAttribute
            {
                Hostname = "localhost",
                QueueName = "queue",
            };

            RabbitMQContext expectedContext = new RabbitMQContext
            {
                ResolvedAttribute = attr,
            };

            Assert.Equal(actualContext.ResolvedAttribute.Hostname, expectedContext.ResolvedAttribute.Hostname);
            Assert.Equal(actualContext.ResolvedAttribute.QueueName, expectedContext.ResolvedAttribute.QueueName);
        }

        [Theory]
        [InlineData("localhost", "queue", null, null)]
        [InlineData(null, "hello", "localhost", null)]
        [InlineData(null, null, "localhost", "name")]
        public void Handles_Null_Attributes_And_Options(string attrHostname, string attrQueueName, string optHostname, string optQueueName)
        {
            RabbitMQAttribute attr = new RabbitMQAttribute
            {
                Hostname = attrHostname,
                QueueName = attrQueueName,
            };

            RabbitMQOptions opt = new RabbitMQOptions
            {
                Hostname = optHostname,
                QueueName = optQueueName,
            };

            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(opt), mockServiceFactory.Object, (ILoggerFactory)loggerFactory);
            var actualContext = config.CreateContext(attr);

            if (optHostname == null && optQueueName == null)
            {
                Assert.Equal(actualContext.ResolvedAttribute.Hostname, attrHostname);
                Assert.Equal(actualContext.ResolvedAttribute.QueueName, attrQueueName);
            }
            else if (attrHostname == null && optQueueName == null)
            {
                Assert.Equal(actualContext.ResolvedAttribute.Hostname, optHostname);
                Assert.Equal(actualContext.ResolvedAttribute.QueueName, attrQueueName);
            }
            else
            {
                Assert.Equal(actualContext.ResolvedAttribute.Hostname, optHostname);
                Assert.Equal(actualContext.ResolvedAttribute.QueueName, optQueueName);
            }
        }

        [Fact]
        public async Task AddAsync_AddsMessagesToQueue()
        {
            var mockRabbitMQService = new Mock<IRabbitMQService>(MockBehavior.Strict);
            var mockBatch = new Mock<IBasicPublishBatch>();
            mockRabbitMQService.Setup(m => m.GetBatch()).Returns(mockBatch.Object);

            var attribute = new RabbitMQAttribute
            {
                Hostname = "localhost",
                QueueName = "queue",
            };

            var context = new RabbitMQContext
            {
                ResolvedAttribute = attribute,
                Service = mockRabbitMQService.Object
            };

            ILoggerFactory loggerFactory = new LoggerFactory();
            ILogger<RabbitMQAsyncCollector> logger = loggerFactory.CreateLogger<RabbitMQAsyncCollector>();
            var collector = new RabbitMQAsyncCollector(context, logger);

            byte[] body = Encoding.UTF8.GetBytes("hi");
            await collector.AddAsync(body);

            mockBatch.Verify(m => m.Add(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), body), Times.Exactly(1));
        }

        [Fact]
        public void Converts_String_Correctly()
        {
            TestClass sampleObj = new TestClass(1, 1);
            string res = JsonConvert.SerializeObject(sampleObj);
            byte[] expectedRes = Encoding.UTF8.GetBytes(res);

            RabbitMQ.RabbitMQExtensionConfigProvider.PocoToBytesConverter<TestClass> converter = new RabbitMQ.RabbitMQExtensionConfigProvider.PocoToBytesConverter<TestClass>();
            byte[] actualRes = converter.Convert(sampleObj);

            Assert.Equal(expectedRes, actualRes);
        }

        public class TestClass
        {
            public int x, y;

            public TestClass(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
    }
}
