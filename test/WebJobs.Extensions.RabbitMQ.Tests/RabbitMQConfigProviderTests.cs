// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions;
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
            var options = new RabbitMQOptions { HostName = Constants.LocalHost, QueueName = "hello" };
            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var mockNameResolver = new Mock<INameResolver>();
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(options), mockNameResolver.Object, mockServiceFactory.Object, (ILoggerFactory)loggerFactory, _emptyConfig);
            var attribute = new RabbitMQAttribute { HostName = "131.107.174.10", QueueName = "queue" };

            var actualContext = config.CreateContext(attribute);

            RabbitMQAttribute attr = new RabbitMQAttribute
            {
                HostName = "131.107.174.10",
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
        [InlineData(Constants.LocalHost, "queue", null, null)]
        [InlineData(null, "hello", Constants.LocalHost, null)]
        [InlineData(null, null, Constants.LocalHost, "name")]
        public void Handles_Null_Queue_Attributes_And_Options(string attrHostname, string attrQueueName, string optHostname, string optQueueName)
        {
            RabbitMQAttribute attr = new RabbitMQAttribute
            {
                HostName = attrHostname,
                QueueName = attrQueueName,
            };

            RabbitMQOptions opt = new RabbitMQOptions
            {
                HostName = optHostname,
                QueueName = optQueueName,
            };

            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var mockNameResolver = new Mock<INameResolver>();
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(opt), mockNameResolver.Object, mockServiceFactory.Object, (ILoggerFactory)loggerFactory, _emptyConfig);
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

        [Theory]
        [InlineData("exchange", "any", "test", null, null, null)]
        [InlineData("exchange", "any", null, null, null, "test")]
        [InlineData("exchange", null, "test", null, "any", null)]
        [InlineData("exchange", null, null, null, "any", "test")]
        [InlineData(null, "any", "test", "exchange", null, null)]
        [InlineData(null, null, "test", "exchange", "any", null)]
        [InlineData(null, "any", null, "exchange", null, "test")]
        [InlineData(null, null, null, "exchange", "any", "test")]
        public void Handles_Null_Exchange_Attributes_And_Options(string attrExchangeName, string attrXMatch, string attrArguments, 
            string optExchangeName, string optXMatch, string optArguments)
        {
            RabbitMQAttribute attr = new RabbitMQAttribute
            {
                ExchangeName = attrExchangeName,
                XMatch = attrXMatch,
                Arguments = attrArguments
            };

            RabbitMQOptions opt = new RabbitMQOptions
            {
                ExchangeName = optExchangeName,
                XMatch = optXMatch,
                Arguments = optArguments
            };

            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var mockNameResolver = new Mock<INameResolver>();
            var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(opt), mockNameResolver.Object, mockServiceFactory.Object, (ILoggerFactory)loggerFactory, _emptyConfig);
            var actualContext = config.CreateContext(attr);

            if (attrExchangeName == null)
            {
                Assert.Equal(actualContext.ResolvedAttribute.ExchangeName, optExchangeName);
            }

            if (attrXMatch == null)
            {
                Assert.Equal(actualContext.ResolvedAttribute.XMatch, optXMatch);
            }

            if (attrArguments == null)
            {
                Assert.Equal(actualContext.ResolvedAttribute.Arguments, optArguments);
            }

            if (optExchangeName == null)
            {
                Assert.Equal(actualContext.ResolvedAttribute.ExchangeName, attrExchangeName);
            }

            if (optXMatch == null)
            {
                Assert.Equal(actualContext.ResolvedAttribute.XMatch, attrXMatch);
            }

            if (optArguments == null)
            {
                Assert.Equal(actualContext.ResolvedAttribute.Arguments, attrArguments);
            }
        }
    }
}
