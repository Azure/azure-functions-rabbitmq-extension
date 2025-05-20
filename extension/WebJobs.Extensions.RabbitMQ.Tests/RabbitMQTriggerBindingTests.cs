// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests;

public class RabbitMQTriggerBindingTests
{
    [Fact]
    public void Verify_BindingDataContract_Types()
    {
        var expectedContract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConsumerTag"] = typeof(string),
            ["DeliveryTag"] = typeof(ulong),
            ["Redelivered"] = typeof(bool),
            ["Exchange"] = typeof(string),
            ["RoutingKey"] = typeof(string),
            ["BasicProperties"] = typeof(IBasicProperties),
            ["Body"] = typeof(ReadOnlyMemory<byte>),
            ["MessageActions"] = typeof(RabbitMQMessageActions),
        };

        IReadOnlyDictionary<string, Type> actualContract = RabbitMQTriggerBinding.CreateBindingDataContract();

        foreach (KeyValuePair<string, Type> item in actualContract)
        {
            Assert.Equal(expectedContract[item.Key], item.Value);
        }
    }

    [Fact]
    public void Verify_BindingDataContract_Values()
    {
        ulong deliveryTag = 1;

        var rand = new Random();
        byte[] buffer = new byte[10];
        rand.NextBytes(buffer);

        ReadOnlyMemory<byte> body = buffer;
        var eventArgs = new BasicDeliverEventArgs("ConsumerName", deliveryTag, false, "n/a", "QueueName", null, body);
        var messageActions = new RabbitMQMessageActions(Mock.Of<IRabbitMQService>());

        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConsumerTag"] = "ConsumerName",
            ["DeliveryTag"] = deliveryTag,
            ["Redelivered"] = false,
            ["RoutingKey"] = "QueueName",
            ["Body"] = body,
            ["Exchange"] = eventArgs.Exchange,
            ["BasicProperties"] = eventArgs.BasicProperties,
            ["MessageActions"] = messageActions,
        };

        IReadOnlyDictionary<string, object> actualContract = RabbitMQTriggerBinding.CreateBindingData(eventArgs, messageActions);

        foreach (KeyValuePair<string, object> item in actualContract)
        {
            Assert.Equal(data[item.Key], item.Value);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RabbitMQTrigger_ManualAck_BasicAckBehavior(bool disableAck)
    {
        // Arrange
        var mockservice = new Mock<IRabbitMQService>();
        var mockModel = new Mock<IModel>();
        mockservice.Setup(a => a.CreateConsumer()).Returns(new AsyncEventingBasicConsumer(mockModel.Object));

        var mockExecutor = new Mock<ITriggeredFunctionExecutor>();
        var mockLogger = new Mock<ILogger>();
        var mockDrainModeManager = new Mock<IDrainModeManager>();
        var mockBasicProperties = new Mock<IBasicProperties>();

        // Simulate successful function execution
        mockExecutor
            .Setup(executor => executor.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));

        var listener = new RabbitMQListener(
            mockservice.Object,
            mockExecutor.Object,
            mockLogger.Object,
            functionId: "test-function",
            queueName: "test-queue",
            disableAck: disableAck,
            prefetchCount: 10,
            drainModeManager: mockDrainModeManager.Object);

        var eventArgs = new BasicDeliverEventArgs
        {
            DeliveryTag = 1,
            Body = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]),
            BasicProperties = mockBasicProperties.Object,
        };

        // Act
        await listener.StartAsync(CancellationToken.None);

        // Find the Consumer instance passed to RabbitMQService.Consume method
        IInvocation consumeInvocation = mockservice.Invocations
            .FirstOrDefault(invocation => invocation.Method.Name == "Consume");

        Assert.NotNull(consumeInvocation);

        var consumer = consumeInvocation.Arguments[2] as AsyncEventingBasicConsumer;
        Assert.NotNull(consumer);

        // Simulate message delivery
        await consumer.HandleBasicDeliver(
            consumerTag: "ctag",
            deliveryTag: eventArgs.DeliveryTag,
            redelivered: false,
            exchange: string.Empty,
            routingKey: string.Empty,
            properties: eventArgs.BasicProperties,
            body: eventArgs.Body.ToArray());

        // Assert
        if (disableAck)
        {
            mockservice.Verify(channel => channel.Acknowledge(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never, "BasicAck should not be called when DisableAck is true.");
        }
        else
        {
            mockservice.Verify(channel => channel.Acknowledge(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once, "BasicAck should be called when DisableAck is false.");
        }
    }
}
