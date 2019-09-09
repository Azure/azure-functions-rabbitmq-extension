// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQListenerTests
    {
        private readonly Mock<ITriggeredFunctionExecutor> _mockExecutor;
        private readonly Mock<IRabbitMQService> _mockService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IRabbitMQModel> _mockModel;

        public RabbitMQListenerTests()
        {
            _mockExecutor = new Mock<ITriggeredFunctionExecutor>();
            _mockService = new Mock<IRabbitMQService>();
            _mockLogger = new Mock<ILogger>();
            _mockModel = new Mock<IRabbitMQModel>();
        }

        [Fact]
        public void CreatesHeadersAndRepublishes()
        {
            _mockService.Setup(m => m.RabbitMQModel).Returns(_mockModel.Object);

            RabbitMQListener listener = new RabbitMQListener(_mockExecutor.Object, _mockService.Object, "blah", 1, _mockLogger.Object);

            var properties = new BasicProperties();
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", properties, Encoding.UTF8.GetBytes("hello world"));
            listener.CreateHeadersAndRepublish(args);

            _mockModel.Verify(m => m.BasicAck(It.IsAny<ulong>(), false), Times.Exactly(1));
            _mockModel.Verify(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Exactly(1));
        }

        [Fact]
        public void RepublishesMessages()
        {
            _mockService.Setup(m => m.RabbitMQModel).Returns(_mockModel.Object);
            RabbitMQListener listener = new RabbitMQListener(_mockExecutor.Object, _mockService.Object, "blah", 1, _mockLogger.Object);

            var properties = new BasicProperties();
            properties.Headers = new Dictionary<string, object>();
            properties.Headers.Add("requeueCount", 1);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", properties, Encoding.UTF8.GetBytes("hello world"));
            listener.RepublishMessages(args);

            _mockModel.Verify(m => m.BasicAck(It.IsAny<ulong>(), false), Times.Exactly(1));
            _mockModel.Verify(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Exactly(1));
        }

        [Fact]
        public void RejectsStaleMessages()
        {
            _mockService.Setup(m => m.RabbitMQModel).Returns(_mockModel.Object);
            RabbitMQListener listener = new RabbitMQListener(_mockExecutor.Object, _mockService.Object, "blah", 1, _mockLogger.Object);

            var properties = new BasicProperties();
            properties.Headers = new Dictionary<string, object>();
            properties.Headers.Add("requeueCount", 6);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", properties, Encoding.UTF8.GetBytes("hello world"));
            listener.RepublishMessages(args);

            _mockModel.Verify(m => m.BasicReject(It.IsAny<ulong>(), false), Times.Exactly(1));
        }
    }
}
