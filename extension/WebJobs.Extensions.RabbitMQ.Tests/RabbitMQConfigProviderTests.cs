// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests;

public class RabbitMQConfigProviderTests
{
    private static readonly IConfiguration EmptyConfig = new ConfigurationBuilder().Build();

    [Fact]
    public void Creates_Context_Correctly()
    {
        var options = new RabbitMQOptions { ConnectionString = "connectionStringFromOptions", QueueName = "queueNameFromOptions" };
        var loggerFactory = new LoggerFactory();
        var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
        var mockNameResolver = new Mock<INameResolver>();
        var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(options), mockNameResolver.Object, mockServiceFactory.Object, loggerFactory, EmptyConfig);
        var attribute = new RabbitMQAttribute { ConnectionStringSetting = "connectionStringSettingFromAttribute", QueueName = "queueNameFromAttributes" };

        RabbitMQContext actualContext = config.CreateContext(attribute);

        Assert.Equal("connectionStringSettingFromAttribute", actualContext.ResolvedAttribute.ConnectionStringSetting);
        Assert.Equal("queueNameFromAttributes", actualContext.ResolvedAttribute.QueueName);
    }

    [Theory]
    [InlineData("connectionStringSettingFromAttribute", "queueNameFromAttribute", null, null)]
    [InlineData(null, "queueNameFromAttribute", "connectionStringFromOptions", null)]
    [InlineData(null, null, "connectionStringFromOptions", "queueNameFromOptions")]
    public void Handles_Null_Attributes_And_Options(string attrConnectionStringSetting, string attrQueueName, string optConnectionString, string optQueueName)
    {
        var attr = new RabbitMQAttribute
        {
            ConnectionStringSetting = attrConnectionStringSetting,
            QueueName = attrQueueName,
        };

        var opt = new RabbitMQOptions
        {
            ConnectionString = optConnectionString,
            QueueName = optQueueName,
        };

        var loggerFactory = new LoggerFactory();
        var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
        var mockNameResolver = new Mock<INameResolver>();
        var config = new RabbitMQExtensionConfigProvider(new OptionsWrapper<RabbitMQOptions>(opt), mockNameResolver.Object, mockServiceFactory.Object, loggerFactory, EmptyConfig);
        RabbitMQContext actualContext = config.CreateContext(attr);

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
