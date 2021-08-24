// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQExtensionConfigProviderTests
    {
        [Fact]
        public void TestConnectionPooling()
        {
            var rabbitmqServiceFactory = new Mock<IRabbitMQServiceFactory>();


            rabbitmqServiceFactory.SetupSequence(a => a.CreateService(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<int>())).Returns(new Mock<IRabbitMQService>().Object)
               .Returns(new Mock<IRabbitMQService>().Object)
               .Returns(new Mock<IRabbitMQService>().Object)
               .Returns(new Mock<IRabbitMQService>().Object);

            rabbitmqServiceFactory.SetupSequence(a => a.CreateService(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<int>())).Returns(new Mock<IRabbitMQService>().Object)
               .Returns(new Mock<IRabbitMQService>().Object)
               .Returns(new Mock<IRabbitMQService>().Object)
               .Returns(new Mock<IRabbitMQService>().Object);

            RabbitMQExtensionConfigProvider extensionConfigProvider = new RabbitMQExtensionConfigProvider(
                new Mock<IOptions<RabbitMQOptions>>().Object,
                new Mock<INameResolver>().Object,
                rabbitmqServiceFactory.Object,
                NullLoggerFactory.Instance,
                new Mock<IConfiguration>().Object);

            var rabbitmqService1 = extensionConfigProvider.GetService("something", "something", "something", "something", 80);
            var rabbitmqService2 = extensionConfigProvider.GetService("something", "something", "something", "something", 80);
            var rabbitmqService3 = extensionConfigProvider.GetService("somethingElse", "something", "something", "something", 80);

            // 1 and 2 should be equal
            Assert.Equal(rabbitmqService1, rabbitmqService2);

            // 3 shouldn't be equal to 1 nor 2
            Assert.NotEqual(rabbitmqService1, rabbitmqService3);
            Assert.NotEqual(rabbitmqService2, rabbitmqService3);

            var rabbitmqService4 = extensionConfigProvider.GetService("asomething", "asomething", "asomething", "asomething", "asomething", 80);
            var rabbitmqService5 = extensionConfigProvider.GetService("asomething", "asomething", "asomething", "asomething", "asomething", 80);
            var rabbitmqService6 = extensionConfigProvider.GetService("asomethingElse", "asomething", "asomething", "asomething", "asomething", 80);

            // 4 and 5 should be equal
            Assert.Equal(rabbitmqService4, rabbitmqService5);

            // 6 shouldn't be equal to 4 or 5
            Assert.NotEqual(rabbitmqService6, rabbitmqService4);
            Assert.NotEqual(rabbitmqService6, rabbitmqService5);
        }
    }
}
