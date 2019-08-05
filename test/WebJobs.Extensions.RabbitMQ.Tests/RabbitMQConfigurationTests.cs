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
using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Trigger;
using RabbitMQ.Client.Events;

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
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(options), _emptyConfig, mockServiceFactory.Object, (ILoggerFactory)loggerFactory);
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
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(opt), _emptyConfig, mockServiceFactory.Object, (ILoggerFactory)loggerFactory);
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

            RabbitMQExtensionConfigProvider.PocoToBytesConverter<TestClass> converter = new RabbitMQExtensionConfigProvider.PocoToBytesConverter<TestClass>();
            byte[] actualRes = converter.Convert(sampleObj);

            Assert.Equal(expectedRes, actualRes);
        }

        // For RabbitMQ Trigger
        [Fact]
        public void Converts_to_Poco()
        {
            TestClass expectedObj = new TestClass(1, 1);

            string objJson = JsonConvert.SerializeObject(expectedObj);
            byte[] objJsonBytes = Encoding.UTF8.GetBytes(objJson);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", null, objJsonBytes);
            RabbitMQExtensionConfigProvider.EventArgsToPocoConverter<TestClass> converter = new RabbitMQExtensionConfigProvider.EventArgsToPocoConverter<TestClass>();
            TestClass actualObj = converter.Convert(args);

            Assert.Equal(expectedObj.x, actualObj.x);
            Assert.Equal(expectedObj.y, actualObj.y);
        }

        [Fact]
        public void Null_Context_Throws_Error()
        {
            var mockProvider = new Mock<RabbitMQTriggerAttributeBindingProvider>();
            Assert.ThrowsAsync<ArgumentNullException>(() => mockProvider.Object.TryCreateAsync(null));
        }

        [Fact]
        public void Creates_Contract_Correctly()
        {
            var expectedContract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            expectedContract.Add("ConsumerTag", typeof(string));
            expectedContract.Add("DeliveryTag", typeof(ulong));
            expectedContract.Add("Redelivered", typeof(bool));
            expectedContract.Add("Exchange", typeof(string));
            expectedContract.Add("RoutingKey", typeof(string));
            expectedContract.Add("BasicProperties", typeof(IBasicProperties));
            expectedContract.Add("Body", typeof(byte[]));

            var actualContract = RabbitMQTriggerBinding.CreateBindingDataContract(); 

            foreach (KeyValuePair<string, Type> item in actualContract)
            {
                Assert.Equal(expectedContract[item.Key], item.Value);
            }
        }

        [Fact]
        public void Correctly_Populates_Binding_Data()
        {
            var data = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);
            data.Add("ConsumerTag", "ConsumerName");
            ulong deliveryTag = 1;
            data.Add("DeliveryTag", deliveryTag);
            data.Add("Redelivered", false);
            data.Add("RoutingKey", "QueueName");

            Random rand = new Random();
            byte[] body = new byte[10];
            rand.NextBytes(body);

            data.Add("Body", body);

            BasicDeliverEventArgs eventArgs = new BasicDeliverEventArgs("ConsumerName", deliveryTag, false, "n/a", "QueueName", null, body);
            data.Add("Exchange", eventArgs.Exchange);
            data.Add("BasicProperties", eventArgs.BasicProperties);

            var actualContract = RabbitMQTriggerBinding.CreateBindingData(eventArgs);

            foreach (KeyValuePair<string, Object> item in actualContract)
            {
                Assert.Equal(data[item.Key], item.Value);
            }
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
