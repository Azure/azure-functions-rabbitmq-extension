// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests;

public class RabbitMQExtensionConfigProviderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DistinctConnectionAndQueuePairsUseDistinctServices(bool reverse)
    {
        const string connection1 = "amqp://guest:guest@localhost/team,alpha";
        const string queue1 = "beta";
        const string connection2 = "amqp://guest:guest@localhost/team";
        const string queue2 = "alpha,beta";
        var factory = new Mock<IRabbitMQServiceFactory>();
        factory.Setup(f => f.CreateService(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<ILogger>()))
            .Returns(() => new Mock<IRabbitMQService>().Object);
        RabbitMQExtensionConfigProvider provider = CreateProvider(factory);

        string triggerConnection = reverse ? connection2 : connection1;
        string triggerQueue = reverse ? queue2 : queue1;
        var output = new RabbitMQAttribute
        {
            ConnectionStringSetting = reverse ? connection1 : connection2,
            QueueName = reverse ? queue1 : queue2,
        };

        IRabbitMQService triggerService = provider.GetService(triggerConnection, triggerQueue, false);
        IRabbitMQService outputService = provider.CreateContext(output).Service;

        Assert.NotSame(triggerService, outputService);
        Assert.Same(triggerService, provider.GetService(triggerConnection, triggerQueue, false));
        Assert.Same(outputService, provider.CreateContext(output).Service);
        factory.Verify(f => f.CreateService(connection1, queue1, false, It.IsAny<ILogger>()), Times.Once);
        factory.Verify(f => f.CreateService(connection2, queue2, false, It.IsAny<ILogger>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClientAndQueueServicesRemainDistinct(bool clientFirst)
    {
        const string clientConnection = "amqp://guest:guest@localhost/vhost,queue";
        const string queueConnection = "amqp://guest:guest@localhost/vhost";
        var factory = new Mock<IRabbitMQServiceFactory>();
        factory.Setup(f => f.CreateService(It.IsAny<string>(), false, It.IsAny<ILogger>()))
            .Returns(() => new Mock<IRabbitMQService>().Object);
        factory.Setup(f => f.CreateService(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<ILogger>()))
            .Returns(() => new Mock<IRabbitMQService>().Object);
        RabbitMQExtensionConfigProvider provider = CreateProvider(factory);

        IRabbitMQService first = clientFirst
            ? provider.GetService(clientConnection, false)
            : provider.GetService(queueConnection, "queue", false);
        IRabbitMQService second = clientFirst
            ? provider.GetService(queueConnection, "queue", false)
            : provider.GetService(clientConnection, false);

        IRabbitMQService repeatedFirst = clientFirst
            ? provider.GetService(clientConnection, false)
            : provider.GetService(queueConnection, "queue", false);
        IRabbitMQService repeatedSecond = clientFirst
            ? provider.GetService(queueConnection, "queue", false)
            : provider.GetService(clientConnection, false);

        Assert.NotSame(first, second);
        Assert.Same(first, repeatedFirst);
        Assert.Same(second, repeatedSecond);
        factory.Verify(f => f.CreateService(clientConnection, false, It.IsAny<ILogger>()), Times.Once);
        factory.Verify(f => f.CreateService(queueConnection, "queue", false, It.IsAny<ILogger>()), Times.Once);
    }

    [Fact]
    public void DifferentCertificateValidationSettingsUseDistinctServices()
    {
        var factory = new Mock<IRabbitMQServiceFactory>();
        factory.Setup(f => f.CreateService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<ILogger>()))
            .Returns(() => new Mock<IRabbitMQService>().Object);
        RabbitMQExtensionConfigProvider provider = CreateProvider(factory);

        IRabbitMQService validated = provider.GetService("connection", "queue", false);
        IRabbitMQService notValidated = provider.GetService("connection", "queue", true);

        Assert.NotSame(validated, notValidated);
        Assert.Same(validated, provider.GetService("connection", "queue", false));
        Assert.Same(notValidated, provider.GetService("connection", "queue", true));
    }

    [Fact]
    public void TestConnectionPooling()
    {
        var rabbitmqServiceFactory = new Mock<IRabbitMQServiceFactory>();

        rabbitmqServiceFactory
            .SetupSequence(a => a.CreateService(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<ILogger>()))
            .Returns(new Mock<IRabbitMQService>().Object);

        rabbitmqServiceFactory
            .SetupSequence(a => a.CreateService(It.IsAny<string>(), false, It.IsAny<ILogger>()))
            .Returns(new Mock<IRabbitMQService>().Object);

        var extensionConfigProvider = new RabbitMQExtensionConfigProvider(
            new Mock<IOptions<RabbitMQOptions>>().Object,
            new Mock<INameResolver>().Object,
            rabbitmqServiceFactory.Object,
            NullLoggerFactory.Instance,
            new Mock<IConfiguration>().Object,
            new Mock<IDrainModeManager>().Object);

        IRabbitMQService rabbitmqService1 = extensionConfigProvider.GetService("something", false);
        IRabbitMQService rabbitmqService2 = extensionConfigProvider.GetService("something", false);
        IRabbitMQService rabbitmqService3 = extensionConfigProvider.GetService("somethingElse", false);

        // 1 and 2 should be equal
        Assert.Equal(rabbitmqService1, rabbitmqService2);

        // 3 shouldn't be equal to 1 nor 2
        Assert.NotEqual(rabbitmqService1, rabbitmqService3);
        Assert.NotEqual(rabbitmqService2, rabbitmqService3);

        IRabbitMQService rabbitmqService4 = extensionConfigProvider.GetService("asomething", "asomething", false);
        IRabbitMQService rabbitmqService5 = extensionConfigProvider.GetService("asomething", "asomething", false);
        IRabbitMQService rabbitmqService6 = extensionConfigProvider.GetService("asomethingElse", "asomething", false);

        // 4 and 5 should be equal
        Assert.Equal(rabbitmqService4, rabbitmqService5);

        // 6 shouldn't be equal to 4 or 5
        Assert.NotEqual(rabbitmqService6, rabbitmqService4);
        Assert.NotEqual(rabbitmqService6, rabbitmqService5);
    }

    private static RabbitMQExtensionConfigProvider CreateProvider(Mock<IRabbitMQServiceFactory> factory)
    {
        return new RabbitMQExtensionConfigProvider(
            Options.Create(new RabbitMQOptions()),
            new Mock<INameResolver>().Object,
            factory.Object,
            NullLoggerFactory.Instance,
            new Mock<IConfiguration>().Object,
            new Mock<IDrainModeManager>().Object);
    }
}
