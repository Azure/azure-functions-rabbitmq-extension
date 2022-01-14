// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using Xunit;
using Constants = Microsoft.Azure.WebJobs.Extensions.Constants;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQClientBuilderTests
    {
        private static readonly IConfiguration _emptyConfig = new ConfigurationBuilder().Build();

        [Fact]
        public void Opens_Connection()
        {
            var options = new OptionsWrapper<RabbitMQOptions>(new RabbitMQOptions { HostName = Constants.LocalHost });
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var config = new RabbitMQExtensionConfigProvider(options, new Mock<INameResolver>().Object, mockServiceFactory.Object, new LoggerFactory(), _emptyConfig);
            mockServiceFactory.Setup(m => m.CreateService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(new Mock<IRabbitMQService>().Object);
            RabbitMQAttribute attr = GetTestAttribute();

            RabbitMQClientBuilder clientBuilder = new RabbitMQClientBuilder(config, options);
            var model = clientBuilder.Convert(attr);

            mockServiceFactory.Verify(m => m.CreateService(It.IsAny<string>(), Constants.LocalHost, "guest", "guest", 5672), Times.Exactly(1));
        }

        [Fact]
        public void TestWhetherConnectionIsPooled()
        {
            var options = new OptionsWrapper<RabbitMQOptions>(new RabbitMQOptions { HostName = Constants.LocalHost });
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            mockServiceFactory.SetupSequence(m => m.CreateService(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(GetRabbitMQService())
                .Returns(GetRabbitMQService());
            var config = new RabbitMQExtensionConfigProvider(options, new Mock<INameResolver>().Object, mockServiceFactory.Object, new LoggerFactory(), _emptyConfig);
            RabbitMQAttribute attr = GetTestAttribute();

            RabbitMQClientBuilder clientBuilder = new RabbitMQClientBuilder(config, options);

            var model = clientBuilder.Convert(attr);
            var model2 = clientBuilder.Convert(attr);

            Assert.Equal(model, model2);
        }

        private RabbitMQAttribute GetTestAttribute()
        {
            return new RabbitMQAttribute
            {
                ConnectionStringSetting = string.Empty,
                HostName = Constants.LocalHost,
                UserName = "guest",
                Password = "guest",
                Port = 5672
            };
        }

        private IRabbitMQService GetRabbitMQService()
        {
            var mockService = new Mock<IRabbitMQService>();
            mockService.Setup(a => a.Model).Returns(
                new Mock<IModel>().Object
            );

            return mockService.Object;
        }
    }
}