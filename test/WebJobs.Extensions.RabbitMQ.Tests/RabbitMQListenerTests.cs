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
        [Fact]
        public void CreatesHeadersAndRepublishes()
        {
            var mockExecutor = new Mock<ITriggeredFunctionExecutor>();
            var mockService = new Mock<IRabbitMQService>();
            var mockLogger = new Mock<ILogger>();
            var mockModel = new Mock<IRabbitMQModel>();
            mockService.Setup(m => m.Model).Returns(mockModel.Object);

            RabbitMQListener listener = new RabbitMQListener(mockExecutor.Object, mockService.Object, "blah", 1, mockLogger.Object);

            var properties = new BasicProperties();
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", properties, Encoding.UTF8.GetBytes("hello world"));
            listener.CreateHeadersAndRepublish(args);

            mockModel.Verify(m => m.BasicAck(It.IsAny<ulong>(), false), Times.Exactly(1));
            mockModel.Verify(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Exactly(1));
        }

        [Fact]
        public void RepublishesMessages()
        {
            var mockExecutor = new Mock<ITriggeredFunctionExecutor>();
            var mockService = new Mock<IRabbitMQService>();
            var mockLogger = new Mock<ILogger>();
            var mockModel = new Mock<IRabbitMQModel>();
            mockService.Setup(m => m.Model).Returns(mockModel.Object);
            RabbitMQListener listener = new RabbitMQListener(mockExecutor.Object, mockService.Object, "blah", 1, mockLogger.Object);

            var properties = new BasicProperties();
            properties.Headers = new Dictionary<string, object>();
            properties.Headers.Add("requeueCount", 1);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", properties, Encoding.UTF8.GetBytes("hello world"));
            listener.RepublishMessages(args);

            mockModel.Verify(m => m.BasicAck(It.IsAny<ulong>(), false), Times.Exactly(1));
            mockModel.Verify(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Exactly(1));
        }

        [Fact]
        public void RejectsStaleMessages()
        {
            var mockExecutor = new Mock<ITriggeredFunctionExecutor>();
            var mockService = new Mock<IRabbitMQService>();
            var mockLogger = new Mock<ILogger>();
            var mockModel = new Mock<IRabbitMQModel>();
            mockService.Setup(m => m.Model).Returns(mockModel.Object);
            RabbitMQListener listener = new RabbitMQListener(mockExecutor.Object, mockService.Object, "blah", 1, mockLogger.Object);

            var properties = new BasicProperties();
            properties.Headers = new Dictionary<string, object>();
            properties.Headers.Add("requeueCount", 6);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", properties, Encoding.UTF8.GetBytes("hello world"));
            listener.RepublishMessages(args);

            mockModel.Verify(m => m.BasicReject(It.IsAny<ulong>(), false), Times.Exactly(1));
        }
    }
}
