// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQConfigProviderTests
    {
        private static readonly IConfiguration _emptyConfig = new ConfigurationBuilder().Build();

        [Fact]
        public void Creates_Context_Correctly()
        {
            var options = new RabbitMQOptions { ConnectionString = "connectionStringFromOptions", QueueName = "queueNameFromOptions" };
            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var mockNameResolver = new Mock<INameResolver>();
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(options), mockNameResolver.Object, mockServiceFactory.Object, (ILoggerFactory)loggerFactory, _emptyConfig);
            var attribute = new RabbitMQAttribute { ConnectionStringSetting = "connectionStringSettingFromAttribute", QueueName = "queueNameFromAttributes" };

            var actualContext = config.CreateContext(attribute);

            Assert.Equal("connectionStringSettingFromAttribute", actualContext.ResolvedAttribute.ConnectionStringSetting);
            Assert.Equal("queueNameFromAttributes", actualContext.ResolvedAttribute.QueueName);
        }

        [Theory]
        [InlineData("connectionStringSettingFromAttribute", "queueNameFromAttribute", null, null)]
        [InlineData(null, "queueNameFromAttribute", "connectionStringFromOptions", null)]
        [InlineData(null, null, "connectionStringFromOptions", "queueNameFromOptions")]
        public void Handles_Null_Attributes_And_Options(string attrConnectionStringSetting, string attrQueueName, string optConnectionString, string optQueueName)
        {
            RabbitMQAttribute attr = new RabbitMQAttribute
            {
                ConnectionStringSetting = attrConnectionStringSetting,
                QueueName = attrQueueName,
            };

            RabbitMQOptions opt = new RabbitMQOptions
            {
                ConnectionString = optConnectionString,
                QueueName = optQueueName,
            };

            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var mockNameResolver = new Mock<INameResolver>();
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(opt), mockNameResolver.Object, mockServiceFactory.Object, (ILoggerFactory)loggerFactory, _emptyConfig);
            var actualContext = config.CreateContext(attr);

            if (optConnectionString == null && optQueueName == null)
            {
                Assert.Equal(attrConnectionStringSetting, actualContext.ResolvedAttribute.ConnectionStringSetting);
                Assert.Equal(attrQueueName, actualContext.ResolvedAttribute.QueueName);
            }
            else if (attrConnectionStringSetting == null && optQueueName == null)
            {
                Assert.Equal(optConnectionString, actualContext.ResolvedAttribute.ConnectionStringSetting);
                Assert.Equal(attrQueueName, actualContext.ResolvedAttribute.QueueName);
            }
            else
            {
                Assert.Equal(optConnectionString, actualContext.ResolvedAttribute.ConnectionStringSetting);
                Assert.Equal(optQueueName, actualContext.ResolvedAttribute.QueueName);
            }
        }
    }
}
