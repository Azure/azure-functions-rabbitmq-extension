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
    using Castle.DynamicProxy.Generators;

    public class RabbitMQClientBuilderTests
    {
        private static readonly IConfiguration _emptyConfig = new ConfigurationBuilder().Build();

        [Fact]
        public void Opens_Connection()
        {
            var options = new OptionsWrapper<RabbitMQOptions>(new RabbitMQOptions { HostName = Constants.LocalHost });
            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var mockNameResolver = new Mock<INameResolver>();
            var config = new RabbitMQExtensionConfigProvider(options, mockNameResolver.Object, mockServiceFactory.Object, loggerFactory, _emptyConfig);
            var mockService = new Mock<IRabbitMQService>();

            mockServiceFactory.Setup(m => m.CreateService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(mockService.Object);

            RabbitMQAttribute attr = new RabbitMQAttribute
            {
                ConnectionStringSetting = string.Empty,
                HostName = Constants.LocalHost,
                UserName = "guest",
                Password = "guest",
                Port = 5672
            };

            RabbitMQClientBuilder clientBuilder = new RabbitMQClientBuilder(config, options);

            var model = clientBuilder.Convert(attr);

            mockServiceFactory.Verify(m => m.CreateService(It.IsAny<string>(), Constants.LocalHost, It.IsAny<string>(), It.IsAny<bool>(), "guest", "guest", 5672, It.IsAny<string>()), Times.Exactly(1));
        }

        [Theory]
        [InlineData(Constants.LocalHost, "guest", "guest", 5672, null)]
        [InlineData(Constants.LocalHost, "guest", "guest", 5672, false)]
        [InlineData(Constants.LocalHost, "guest", "guest", 5672, true)]
        public void Opens_Connection_QueueDurable(string hostName, string userName, string password, int port, bool? queueDurable)
        {
            var options = new OptionsWrapper<RabbitMQOptions>(new RabbitMQOptions { HostName = Constants.LocalHost });
            var loggerFactory = new LoggerFactory();
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var mockNameResolver = new Mock<INameResolver>();
            var config = new RabbitMQExtensionConfigProvider(options, mockNameResolver.Object, mockServiceFactory.Object, loggerFactory, _emptyConfig);
            var mockService = new Mock<IRabbitMQService>();

            mockServiceFactory.Setup(m => m.CreateService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(mockService.Object);

            RabbitMQAttribute attr = new RabbitMQAttribute
                                     {
                                         ConnectionStringSetting = string.Empty,
                                         HostName = hostName,
                                         UserName = userName,
                                         Password = password,
                                         Port = port
                                     };
            if (queueDurable.HasValue)
            {
                attr.QueueDurable = queueDurable.Value;
            }

            RabbitMQClientBuilder clientBuilder = new RabbitMQClientBuilder(config, options);

            var model = clientBuilder.Convert(attr);

            mockServiceFactory.Verify(m => m.CreateService(It.IsAny<string>(), hostName, It.IsAny<string>(), queueDurable ?? false, userName, password, port, It.IsAny<string>()), Times.Exactly(1));
        }
    }
}
