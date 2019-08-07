// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQOutputBindingTests
    {
        [Fact]
        public void Creates_Context_Correctly()
        {
            var options = new RabbitMQOptions { Hostname = "localhost", QueueName = "hello" };
            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var mockNameResolver = new Mock<INameResolver>();
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(options), mockNameResolver.Object, mockServiceFactory.Object, (ILoggerFactory)loggerFactory);
            var attribute = new RabbitMQAttribute { HostName = "localhost", QueueName = "queue" };

            var actualContext = config.CreateContext(attribute);

            RabbitMQAttribute attr = new RabbitMQAttribute
            {
                HostName = "localhost",
                QueueName = "queue",
            };

            RabbitMQContext expectedContext = new RabbitMQContext
            {
                ResolvedAttribute = attr,
            };

            Assert.Equal(actualContext.ResolvedAttribute.HostName, expectedContext.ResolvedAttribute.HostName);
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
                HostName = attrHostname,
                QueueName = attrQueueName,
            };

            RabbitMQOptions opt = new RabbitMQOptions
            {
                Hostname = optHostname,
                QueueName = optQueueName,
            };

            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var mockNameResolver = new Mock<INameResolver>();
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(opt), mockNameResolver.Object, mockServiceFactory.Object, (ILoggerFactory)loggerFactory);
            var actualContext = config.CreateContext(attr);

            if (optHostname == null && optQueueName == null)
            {
                Assert.Equal(actualContext.ResolvedAttribute.HostName, attrHostname);
                Assert.Equal(actualContext.ResolvedAttribute.QueueName, attrQueueName);
            }
            else if (attrHostname == null && optQueueName == null)
            {
                Assert.Equal(actualContext.ResolvedAttribute.HostName, optHostname);
                Assert.Equal(actualContext.ResolvedAttribute.QueueName, attrQueueName);
            }
            else
            {
                Assert.Equal(actualContext.ResolvedAttribute.HostName, optHostname);
                Assert.Equal(actualContext.ResolvedAttribute.QueueName, optQueueName);
            }
        }

        [Fact]
        public void Converts_String_Correctly()
        {
            TestClass sampleObj = new TestClass(1, 1);
            string res = JsonConvert.SerializeObject(sampleObj);
            byte[] expectedRes = Encoding.UTF8.GetBytes(res);

            PocoToBytesConverter<TestClass> converter = new PocoToBytesConverter<TestClass>();
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
