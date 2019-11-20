// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQListenerTests
    {
        private readonly Mock<ITriggeredFunctionExecutor> _mockExecutor;
        private readonly Mock<IRabbitMQService> _mockService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IRabbitMQModel> _mockModel;
        private readonly Mock<FunctionDescriptor> _mockDescriptor;
        private readonly RabbitMQListener _testListener;

        public RabbitMQListenerTests()
        {
            _mockExecutor = new Mock<ITriggeredFunctionExecutor>();
            _mockService = new Mock<IRabbitMQService>();
            _mockLogger = new Mock<ILogger>();
            _mockModel = new Mock<IRabbitMQModel>();
            _mockDescriptor = new Mock<FunctionDescriptor>();

            _mockService.Setup(m => m.RabbitMQModel).Returns(_mockModel.Object);

            QueueDeclareOk queueInfo = new QueueDeclareOk("blah", 5, 1);
            _mockModel.Setup(m => m.QueueDeclarePassive(It.IsAny<string>())).Returns(queueInfo);

            _testListener = new RabbitMQListener(_mockExecutor.Object, _mockService.Object, "blah", _mockLogger.Object, new FunctionDescriptor { Id = "TestFunction" });
        }

        [Fact]
        public void CreatesHeadersAndRepublishes()
        {
            _mockService.Setup(m => m.RabbitMQModel).Returns(_mockModel.Object);

            RabbitMQListener listener = new RabbitMQListener(_mockExecutor.Object, _mockService.Object, "blah", _mockLogger.Object, _mockDescriptor.Object);

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
            RabbitMQListener listener = new RabbitMQListener(_mockExecutor.Object, _mockService.Object, "blah", _mockLogger.Object, _mockDescriptor.Object);

            var properties = new BasicProperties()
            {
                Headers = new Dictionary<string, object>() { { "requeueCount", 1 } }
            };
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", properties, Encoding.UTF8.GetBytes("hello world"));
            listener.RepublishMessages(args);

            _mockModel.Verify(m => m.BasicAck(It.IsAny<ulong>(), false), Times.Exactly(1));
            _mockModel.Verify(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Exactly(1));
        }

        [Fact]
        public void RejectsStaleMessages()
        {
            _mockService.Setup(m => m.RabbitMQModel).Returns(_mockModel.Object);
            RabbitMQListener listener = new RabbitMQListener(_mockExecutor.Object, _mockService.Object, "blah", _mockLogger.Object, _mockDescriptor.Object);

            var properties = new BasicProperties()
            {
                Headers = new Dictionary<string, object>() { { "requeueCount", 6 } }
            };
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", properties, Encoding.UTF8.GetBytes("hello world"));
            listener.RepublishMessages(args);

            _mockModel.Verify(m => m.BasicReject(It.IsAny<ulong>(), false), Times.Exactly(1));
        }

        [Fact]
        public void ScaleMonitor_Id_ReturnsExpectedValue()
        {
            Assert.Equal("testfunction-rabbitmqtrigger-blah", _testListener.Descriptor.Id);
        }

        [Fact]
        public async Task GetMetrics_ReturnsExpectedResult()
        {
            RabbitMQListener listener = new RabbitMQListener(_mockExecutor.Object, _mockService.Object, "listener_test_queue", _mockLogger.Object, new FunctionDescriptor { Id = "TestFunction" });
            var metrics = await listener.GetMetricsAsync();

            Assert.Equal((uint)5, metrics.QueueLength);
            Assert.NotEqual(default(DateTime), metrics.Timestamp);
        }

        [Fact]
        public void GetScaleStatus_NoMetrics_ReturnsVote_None()
        {
            var context = new ScaleStatusContext<RabbitMQTriggerMetrics>
            {
                WorkerCount = 1
            };

            var status = _testListener.GetScaleStatus(context);
            Assert.Equal(ScaleVote.None, status.Vote);

            status = ((IScaleMonitor)_testListener).GetScaleStatus(context);
            Assert.Equal(ScaleVote.None, status.Vote);
        }

        [Fact]
        public void GetScaleStatus_MessagesPerWorkerThresholdExceeded_ReturnsVote_ScaleOut()
        {
            var context = new ScaleStatusContext<RabbitMQTriggerMetrics>
            {
                WorkerCount = 1
            };

            var Timestamp = DateTime.UtcNow;
            var RabbitMQTriggerMetrics = new List<RabbitMQTriggerMetrics>
            {
                new RabbitMQTriggerMetrics { QueueLength = 2500, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 2505, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 2612, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 2700, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 2810, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 2900, Timestamp = Timestamp.AddSeconds(15) }
            };
            context.Metrics = RabbitMQTriggerMetrics;

            var status = _testListener.GetScaleStatus(context);

            Assert.Equal(ScaleVote.ScaleOut, status.Vote);

            // verify again with a non generic context instance
            var context2 = new ScaleStatusContext
            {
                WorkerCount = 1,
                Metrics = RabbitMQTriggerMetrics
            };
            status = ((IScaleMonitor)_testListener).GetScaleStatus(context2);
            Assert.Equal(ScaleVote.ScaleOut, status.Vote);
        }

        [Fact]
        public void GetScaleStatus_QueueLengthIncreasing_ReturnsVote_ScaleOut()
        {
            var context = new ScaleStatusContext<RabbitMQTriggerMetrics>
            {
                WorkerCount = 1
            };
            var Timestamp = DateTime.UtcNow;
            context.Metrics = new List<RabbitMQTriggerMetrics>
            {
                new RabbitMQTriggerMetrics { QueueLength = 10, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 20, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 40, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 80, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 100, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 150, Timestamp = Timestamp.AddSeconds(15) }
            };

            var status = _testListener.GetScaleStatus(context);

            Assert.Equal(ScaleVote.ScaleOut, status.Vote);
        }

        [Fact]
        public void GetScaleStatus_QueueLengthDecreasing_ReturnsVote_ScaleIn()
        {
            var context = new ScaleStatusContext<RabbitMQTriggerMetrics>
            {
                WorkerCount = 5
            };
            var Timestamp = DateTime.UtcNow;
            context.Metrics = new List<RabbitMQTriggerMetrics>
            {
                new RabbitMQTriggerMetrics { QueueLength = 150, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 100, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 80, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 40, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 20, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 10, Timestamp = Timestamp.AddSeconds(15) }
            };

            var status = _testListener.GetScaleStatus(context);

            Assert.Equal(ScaleVote.ScaleIn, status.Vote);
        }

        [Fact]
        public void GetScaleStatus_QueueSteady_ReturnsVote_None()
        {
            var context = new ScaleStatusContext<RabbitMQTriggerMetrics>
            {
                WorkerCount = 2
            };
            var Timestamp = DateTime.UtcNow;
            context.Metrics = new List<RabbitMQTriggerMetrics>
            {
                new RabbitMQTriggerMetrics { QueueLength = 1500, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 1600, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 1400, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 1300, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 1700, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 1600, Timestamp = Timestamp.AddSeconds(15) }
            };

            var status = _testListener.GetScaleStatus(context);

            Assert.Equal(ScaleVote.None, status.Vote);
        }

        [Fact]
        public void GetScaleStatus_QueueIdle_ReturnsVote_ScaleOut()
        {
            var context = new ScaleStatusContext<RabbitMQTriggerMetrics>
            {
                WorkerCount = 3
            };
            var Timestamp = DateTime.UtcNow;
            context.Metrics = new List<RabbitMQTriggerMetrics>
            {
                new RabbitMQTriggerMetrics { QueueLength = 0, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 0, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 0, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 0, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 0, Timestamp = Timestamp.AddSeconds(15) },
                new RabbitMQTriggerMetrics { QueueLength = 0, Timestamp = Timestamp.AddSeconds(15) }
            };

            var status = _testListener.GetScaleStatus(context);

            Assert.Equal(ScaleVote.ScaleIn, status.Vote);
        }

        [Fact]
        public void GetScaleStatus_UnderSampleCountThreshold_ReturnsVote_None()
        {
            var context = new ScaleStatusContext<RabbitMQTriggerMetrics>
            {
                WorkerCount = 1
            };
            context.Metrics = new List<RabbitMQTriggerMetrics>
            {
                new RabbitMQTriggerMetrics { QueueLength = 5, Timestamp = DateTime.UtcNow },
                new RabbitMQTriggerMetrics { QueueLength = 10, Timestamp = DateTime.UtcNow }
            };

            var status = _testListener.GetScaleStatus(context);

            Assert.Equal(ScaleVote.None, status.Vote);
        }
    }
}
