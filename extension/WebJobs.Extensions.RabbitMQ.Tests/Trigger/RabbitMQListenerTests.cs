// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests;

public class RabbitMQListenerTests
{
    private readonly Mock<IRabbitMQService> mockService;
    private readonly Mock<IModel> mockModel;

    public RabbitMQListenerTests()
    {
        this.mockService = new Mock<IRabbitMQService>();
        this.mockModel = new Mock<IModel>();

        this.mockService.Setup(m => m.Model).Returns(this.mockModel.Object);
        var queueInfo = new QueueDeclareOk("blah", 5, 1);
        this.mockModel.Setup(m => m.QueueDeclarePassive(It.IsAny<string>())).Returns(queueInfo);
    }

    // Created https://github.com/Azure/azure-functions-rabbitmq-extension/issues/214 for re-enabling the unit tests.
    // Currently, the tests are testing private-made-internal class methods, which hinders refactoring of the logic
    // within the class. Instead, the aim should be to test the functionality through public methods only.

    //// [Fact]
    //// public void CreatesHeadersAndRepublishes()
    //// {
    ////     string queueName = "blah";
    ////     this.mockService.Setup(m => m.RabbitMQModel).Returns(this.mockModel.Object);
    ////     var listener = new RabbitMQListener(this.mockExecutor.Object, this.mockService.Object, queueName, this.mockLogger.Object, this.mockDescriptor.Object, 30);

    ////     IBasicProperties properties = Mock.Of<IBasicProperties>();
    ////     var args = new BasicDeliverEventArgs("tag", 1, false, string.Empty, "routingKey", properties, Encoding.UTF8.GetBytes("hello world"));
    ////     listener.CreateHeadersAndRepublish(args);

    ////     this.mockModel.Verify(m => m.BasicAck(It.IsAny<ulong>(), false), Times.Exactly(1));
    ////     this.mockModel.Verify(m => m.BasicPublish(It.IsAny<string>(), queueName, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()), Times.Exactly(1));
    //// }

    //// [Fact]
    //// public void RepublishesMessages()
    //// {
    ////     string queueName = "blah";
    ////     this.mockService.Setup(m => m.RabbitMQModel).Returns(this.mockModel.Object);
    ////     var listener = new RabbitMQListener(this.mockExecutor.Object, this.mockService.Object, queueName, this.mockLogger.Object, this.mockDescriptor.Object, 30);

    ////     IBasicProperties properties = Mock.Of<IBasicProperties>(property => property.Headers == new Dictionary<string, object>() { { "requeueCount", 1 } });
    ////     var args = new BasicDeliverEventArgs("tag", 1, false, string.Empty, "routingKey", properties, Encoding.UTF8.GetBytes("hello world"));
    ////     listener.RepublishMessages(args);

    ////     this.mockModel.Verify(m => m.BasicAck(It.IsAny<ulong>(), false), Times.Exactly(1));
    ////     this.mockModel.Verify(m => m.BasicPublish(It.IsAny<string>(), queueName, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()), Times.Exactly(1));
    //// }

    //// [Fact]
    //// public void RejectsStaleMessages()
    //// {
    ////     this.mockService.Setup(m => m.RabbitMQModel).Returns(this.mockModel.Object);
    ////     var listener = new RabbitMQListener(this.mockExecutor.Object, this.mockService.Object, "blah", this.mockLogger.Object, this.mockDescriptor.Object, 30);

    ////     IBasicProperties properties = Mock.Of<IBasicProperties>(property => property.Headers == new Dictionary<string, object>() { { "requeueCount", 6 } });
    ////     var args = new BasicDeliverEventArgs("tag", 1, false, string.Empty, "queue", properties, Encoding.UTF8.GetBytes("hello world"));
    ////     listener.RepublishMessages(args);

    ////     this.mockModel.Verify(m => m.BasicReject(It.IsAny<ulong>(), false), Times.Exactly(1));
    //// }

    /// <summary>
    /// Verifies that the scale monitor descriptor ID is set to expected value.
    /// </summary>
    [Theory]
    [InlineData("testUserFunctionId", "testQueueName", "testUserFunctionId-RabbitMQTrigger-testQueueName")]
    [InlineData("тестПользовательФункцияИд", "тестОчередьИмя", "тестПользовательФункцияИд-RabbitMQTrigger-тестОчередьИмя")]
    public void ScaleMonitorDescriptor_ReturnsExpectedValue(string functionId, string queueName, string expectedDescriptorId)
    {
        IScaleMonitor<RabbitMQTriggerMetrics> monitor = GetScaleMonitor(functionId, queueName);
        Assert.Equal(expectedDescriptorId, monitor.Descriptor.Id);
    }

    /// <summary>
    /// Verifies that no-scaling is requested if there are insufficient metrics available for making the scale decision.
    /// </summary>
    [Theory]
    [InlineData(null)] // metrics == null
    [InlineData(new uint[] { })] // metrics.Length == 0
    [InlineData(new uint[] { 1000, 1000, 1000, 1000 })] // metrics.Length == 4.
    public void ScaleMonitorGetScaleStatus_InsufficentMetrics_ReturnsNone(uint[] messageCounts)
    {
        (IScaleMonitor<RabbitMQTriggerMetrics> monitor, List<string> logMessages) = GetScaleMonitor();
        ScaleStatusContext context = GetScaleStatusContext(messageCounts, 0);

        ScaleStatus scaleStatus = monitor.GetScaleStatus(context);

        Assert.Equal(ScaleVote.None, scaleStatus.Vote);
        Assert.Contains("Requesting no-scaling: Insufficient metrics for making scale decision for function: 'testFunctionId', queue: 'testQueueName'.", logMessages);
    }

    /// <summary>
    /// Verifies that only the most recent samples are considered for making the scale decision.
    /// </summary>
    [Theory]
    [InlineData(new uint[] { 0, 0, 4, 3, 2, 0 }, 2, ScaleVote.None)]
    [InlineData(new uint[] { 0, 0, 4, 3, 2, 1, 0 }, 2, ScaleVote.ScaleIn)]
    [InlineData(new uint[] { 1000, 1000, 0, 1, 2, 1000 }, 1, ScaleVote.None)]
    [InlineData(new uint[] { 1000, 1000, 0, 1, 2, 3, 1000 }, 1, ScaleVote.ScaleOut)]
    public void ScaleMonitorGetScaleStatus_ExcessMetrics_IgnoresExcessMetrics(uint[] messageCounts, int workerCount, ScaleVote scaleVote)
    {
        (IScaleMonitor<RabbitMQTriggerMetrics> monitor, _) = GetScaleMonitor();
        ScaleStatusContext context = GetScaleStatusContext(messageCounts, workerCount);

        ScaleStatus scaleStatus = monitor.GetScaleStatus(context);

        Assert.Equal(scaleVote, scaleStatus.Vote);
    }

    /// <summary>
    /// Verifies that scale-out is requested if the latest count of messages is above the combined limit of all workers.
    /// </summary>
    [Theory]
    [InlineData(new uint[] { 0, 0, 0, 0, 1 }, 0)]
    [InlineData(new uint[] { 0, 0, 0, 0, 1001 }, 1)]
    [InlineData(new uint[] { 0, 0, 0, 0, 10001 }, 10)]
    public void ScaleMonitorGetScaleStatus_LastCountAboveLimit_ReturnsScaleOut(uint[] messageCounts, int workerCount)
    {
        (IScaleMonitor<RabbitMQTriggerMetrics> monitor, List<string> logMessages) = GetScaleMonitor();
        ScaleStatusContext context = GetScaleStatusContext(messageCounts, workerCount);

        ScaleStatus scaleStatus = monitor.GetScaleStatus(context);

        Assert.Equal(ScaleVote.ScaleOut, scaleStatus.Vote);
        Assert.Contains("Requesting scale-out: Found too many messages for function: 'testFunctionId', queue: 'testQueueName' relative to the number of workers.", logMessages);
    }

    /// <summary>
    /// Verifies that no-scaling is requested if the latest count of messages is not above the combined limit of all
    /// workers.
    /// </summary>
    [Theory]
    [InlineData(new uint[] { 0, 0, 0, 0, 0 }, 0)]
    [InlineData(new uint[] { 0, 0, 0, 0, 1000 }, 1)]
    [InlineData(new uint[] { 0, 0, 0, 0, 10000 }, 10)]
    public void ScaleMonitorGetScaleStatus_LastCountBelowLimit_ReturnsNone(uint[] messageCounts, int workerCount)
    {
        (IScaleMonitor<RabbitMQTriggerMetrics> monitor, List<string> logMessages) = GetScaleMonitor();
        ScaleStatusContext context = GetScaleStatusContext(messageCounts, workerCount);

        ScaleStatus scaleStatus = monitor.GetScaleStatus(context);

        Assert.Equal(ScaleVote.None, scaleStatus.Vote);
        Assert.Contains("Requesting no-scaling: Found function: 'testFunctionId', queue: 'testQueueName' to not require scaling.", logMessages);
    }

    /// <summary>
    /// Verifies that scale-out is requested if the count of messages is strictly increasing and may exceed the combined
    /// limit of all workers. Since the metric samples are separated by 10 seconds, the existing implementation should
    /// only consider the last three samples in its calculation.
    /// </summary>
    [Theory]
    [InlineData(new uint[] { 0, 1, 500, 501, 751 }, 1)]
    [InlineData(new uint[] { 0, 1, 4999, 5001, 7500 }, 10)]
    public void ScaleMonitorGetScaleStatus_CountIncreasingAboveLimit_ReturnsScaleOut(uint[] messageCounts, int workerCount)
    {
        (IScaleMonitor<RabbitMQTriggerMetrics> monitor, List<string> logMessages) = GetScaleMonitor();
        ScaleStatusContext context = GetScaleStatusContext(messageCounts, workerCount);

        ScaleStatus scaleStatus = monitor.GetScaleStatus(context);

        Assert.Equal(ScaleVote.ScaleOut, scaleStatus.Vote);
        Assert.Contains("Requesting scale-out: Found the messages for function: 'testFunctionId', queue: 'testQueueName' to be continuously increasing and may exceed the maximum limit set for the workers.", logMessages);
    }

    /// <summary>
    /// Verifies that no-scaling is requested if the count of messages is strictly increasing but it may still stay
    /// below the combined limit of all workers. Since the metric samples are separated by 10 seconds, the existing
    /// implementation should only consider the last three samples in its calculation.
    /// </summary>
    [Theory]
    [InlineData(new uint[] { 0, 1, 500, 501, 750 }, 1)]
    [InlineData(new uint[] { 0, 1, 5000, 5001, 7500 }, 10)]
    public void ScaleMonitorGetScaleStatus_CountIncreasingBelowLimit_ReturnsNone(uint[] messageCounts, int workerCount)
    {
        (IScaleMonitor<RabbitMQTriggerMetrics> monitor, List<string> logMessages) = GetScaleMonitor();
        ScaleStatusContext context = GetScaleStatusContext(messageCounts, workerCount);

        ScaleStatus scaleStatus = monitor.GetScaleStatus(context);

        Assert.Equal(ScaleVote.None, scaleStatus.Vote);
        Assert.Contains("Avoiding scale-out: Found the messages for function: 'testFunctionId', queue: 'testQueueName' to be increasing but they may not exceed the maximum limit set for the workers.", logMessages);
    }

    /// <summary>
    /// Verifies that scale-in is requested if the count of messages is strictly decreasing (or zero) and is also below
    /// the combined limit of workers after being reduced by one.
    /// </summary>
    [Theory]
    [InlineData(new uint[] { 0, 0, 0, 0, 0 }, 1)]
    [InlineData(new uint[] { 1, 0, 0, 0, 0 }, 1)]
    [InlineData(new uint[] { 5, 4, 3, 2, 0 }, 1)]
    [InlineData(new uint[] { 9005, 9004, 9003, 9002, 9000 }, 10)]
    public void ScaleMonitorGetScaleStatus_CountDecreasingBelowLimit_ReturnsScaleIn(uint[] messageCounts, int workerCount)
    {
        (IScaleMonitor<RabbitMQTriggerMetrics> monitor, List<string> logMessages) = GetScaleMonitor();
        ScaleStatusContext context = GetScaleStatusContext(messageCounts, workerCount);

        ScaleStatus scaleStatus = monitor.GetScaleStatus(context);

        Assert.Equal(ScaleVote.ScaleIn, scaleStatus.Vote);
        Assert.Contains("Requesting scale-in: Found function: 'testFunctionId', queue: 'testQueueName' to be either idle or the messages to be continuously decreasing.", logMessages);
    }

    /// <summary>
    /// Verifies that scale-in is requested if the count of messages is strictly decreasing (or zero) but it is still
    /// above the combined limit of workers after being reduced by one.
    /// </summary>
    [Theory]
    [InlineData(new uint[] { 5, 4, 3, 2, 1 }, 1)]
    [InlineData(new uint[] { 9005, 9004, 9003, 9002, 9001 }, 10)]
    public void ScaleMonitorGetScaleStatus_CountDecreasingAboveLimit_ReturnsNone(uint[] messageCounts, int workerCount)
    {
        (IScaleMonitor<RabbitMQTriggerMetrics> monitor, List<string> logMessages) = GetScaleMonitor();
        ScaleStatusContext context = GetScaleStatusContext(messageCounts, workerCount);

        ScaleStatus scaleStatus = monitor.GetScaleStatus(context);

        Assert.Equal(ScaleVote.None, scaleStatus.Vote);
        Assert.Contains("Avoiding scale-in: Found the messages for function: 'testFunctionId', queue: 'testQueueName' to be decreasing but they are high enough to require all existing workers for processing.", logMessages);
    }

    /// <summary>
    /// Verifies that no-scaling is requested if the count of messages is neither strictly increasing and nor strictly
    /// decreasing.
    /// </summary>
    [Theory]
    [InlineData(new uint[] { 0, 0, 1, 2, 3 }, 1)]
    [InlineData(new uint[] { 1, 1, 0, 0, 0 }, 10)]
    public void ScaleMonitorGetScaleStatus_CountNotIncreasingOrDecreasing_ReturnsNone(uint[] messageCounts, int workerCount)
    {
        (IScaleMonitor<RabbitMQTriggerMetrics> monitor, List<string> logMessages) = GetScaleMonitor();
        ScaleStatusContext context = GetScaleStatusContext(messageCounts, workerCount);

        ScaleStatus scaleStatus = monitor.GetScaleStatus(context);

        Assert.Equal(ScaleVote.None, scaleStatus.Vote);

        // Ensure that no-scaling was not requested because of other conditions.
        Assert.DoesNotContain("Avoiding scale-out: Found the messages for function: 'testFunctionId', queue: 'testQueueName' to be increasing but they may not exceed the maximum limit set for the workers.", logMessages);
        Assert.DoesNotContain("Avoiding scale-in: Found the messages for function: 'testFunctionId', queue: 'testQueueName' to be decreasing but they are high enough to require all existing workers for processing.", logMessages);
        Assert.Contains("Requesting no-scaling: Found function: 'testFunctionId', queue: 'testQueueName' to not require scaling.", logMessages);
    }

    private static IScaleMonitor<RabbitMQTriggerMetrics> GetScaleMonitor(string functionId, string queueName)
    {
        return new RabbitMQListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Mock.Of<IModel>(),
            Mock.Of<ILogger>(),
            functionId,
            queueName,
            7357);
    }

    private static (IScaleMonitor<RabbitMQTriggerMetrics> Monitor, List<string> LogMessages) GetScaleMonitor()
    {
        (Mock<ILogger> mockLogger, List<string> logMessages) = CreateMockLogger();

        IScaleMonitor<RabbitMQTriggerMetrics> monitor = new RabbitMQListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Mock.Of<IModel>(),
            mockLogger.Object,
            "testFunctionId",
            "testQueueName",
            7357);

        return (monitor, logMessages);
    }

    private static ScaleStatusContext GetScaleStatusContext(uint[] messageCounts, int workerCount)
    {
        DateTime now = DateTime.UtcNow;

        // Returns metric samples separated by 10 seconds. The time-difference is essential for testing the scale-out logic.
        return new ScaleStatusContext
        {
            Metrics = messageCounts?.Select((count, index) => new RabbitMQTriggerMetrics
            {
                MessageCount = count,
                Timestamp = now + TimeSpan.FromSeconds(10 * index),
            }),
            WorkerCount = workerCount,
        };
    }

    private static (Mock<ILogger> Logger, List<string> LogMessages) CreateMockLogger()
    {
        // Since multiple threads are not involved when computing the scale-status, it should be okay to not use
        // a thread-safe collection for storing the log messages.
        var logMessages = new List<string>();
        var mockLogger = new Mock<ILogger>();

        // Both LogInformation() and LogDebug() are extension (static) methods and cannot be mocked. Hence, we need to
        // setup callback on an inner class method that gets eventually called by these methods in order to extract
        // the log message.
        mockLogger
            .Setup(logger => logger.Log(It.IsAny<LogLevel>(), 0, It.IsAny<FormattedLogValues>(), null, It.IsAny<Func<object, Exception, string>>()))
            .Callback((LogLevel logLevel, EventId eventId, object state, Exception exception, Func<object, Exception, string> formatter) =>
            {
                logMessages.Add(state.ToString());
            });

        return (mockLogger, logMessages);
    }
}
