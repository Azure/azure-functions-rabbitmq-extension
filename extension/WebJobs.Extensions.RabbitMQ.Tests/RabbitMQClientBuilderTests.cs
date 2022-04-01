// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQClientBuilderTests
    {
        private static readonly IConfiguration _emptyConfig = new ConfigurationBuilder().Build();

        [Fact]
        public void Opens_Connection()
        {
            var options = new OptionsWrapper<RabbitMQOptions>(new RabbitMQOptions());
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            var config = new RabbitMQExtensionConfigProvider(options, new Mock<INameResolver>().Object, mockServiceFactory.Object, new LoggerFactory(), _emptyConfig);
            mockServiceFactory.Setup(m => m.CreateService(It.IsAny<string>(), false)).Returns(new Mock<IRabbitMQService>().Object);
            RabbitMQAttribute attr = GetTestAttribute();

            var clientBuilder = new RabbitMQClientBuilder(config, options);
            IModel model = clientBuilder.Convert(attr);

            mockServiceFactory.Verify(m => m.CreateService(It.IsAny<string>(), false), Times.Exactly(1));
        }

        [Fact]
        public void TestWhetherConnectionIsPooled()
        {
            var options = new OptionsWrapper<RabbitMQOptions>(new RabbitMQOptions());
            var mockServiceFactory = new Mock<IRabbitMQServiceFactory>();
            mockServiceFactory.SetupSequence(m => m.CreateService(It.IsAny<string>(), false))
                .Returns(GetRabbitMQService());
            var config = new RabbitMQExtensionConfigProvider(options, new Mock<INameResolver>().Object, mockServiceFactory.Object, new LoggerFactory(), _emptyConfig);
            RabbitMQAttribute attr = GetTestAttribute();

            var clientBuilder = new RabbitMQClientBuilder(config, options);

            IModel model = clientBuilder.Convert(attr);
            IModel model2 = clientBuilder.Convert(attr);

            Assert.Equal(model, model2);
        }

        private static RabbitMQAttribute GetTestAttribute()
        {
            return new RabbitMQAttribute
            {
                ConnectionStringSetting = "amqp://guest:guest@localhost:5672",
            };
        }

        private static IRabbitMQService GetRabbitMQService()
        {
            var mockService = new Mock<IRabbitMQService>();
            mockService.Setup(a => a.Model).Returns(new Mock<IModel>().Object);
            return mockService.Object;
        }
    }
}
