// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.EndToEndTests;

/// <summary>
/// End-to-end tests for RabbitMQ trigger and output bindings.
/// </summary>
[Collection(RabbitMQE2ETestFixtureDefinition.Name)]
public class RabbitMQEndToEndTests
{
    private readonly RabbitMQEndToEndTestFixture _fixture;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    public RabbitMQEndToEndTests(RabbitMQEndToEndTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StringValueSingleTriggerReceivesMessage()
    {
        // Arrange
        _fixture.ClearReceivedMessages();
        _fixture.DeclareQueue(TriggerFunctions.StringTriggerQueueName);

        var testMessage = $"Test message {Guid.NewGuid()}";

        // Act
        _fixture.PublishMessage(TriggerFunctions.StringTriggerQueueName, testMessage);

        // Assert
        var received = await _fixture.WaitForMessagesAsync(1, _defaultTimeout);
        Assert.True(received, "Timed out waiting for message");
        Assert.Single(_fixture.ReceivedMessages);
        Assert.Equal(testMessage, _fixture.ReceivedMessages[0]);
    }

    [Fact]
    public async Task ByteArrayTriggerReceivesMessage()
    {
        // Arrange
        _fixture.ClearReceivedMessages();
        _fixture.DeclareQueue(TriggerFunctions.ByteArrayTriggerQueueName);

        var testMessage = $"Byte array test {Guid.NewGuid()}";

        // Act
        _fixture.PublishMessage(TriggerFunctions.ByteArrayTriggerQueueName, testMessage);

        // Assert
        var received = await _fixture.WaitForMessagesAsync(1, _defaultTimeout);
        Assert.True(received, "Timed out waiting for message");
        Assert.Single(_fixture.ReceivedMessages);
        Assert.Equal(testMessage, _fixture.ReceivedMessages[0]);
    }

    [Fact]
    public async Task PocoTriggerDeserializesMessage()
    {
        // Arrange
        _fixture.ClearReceivedMessages();
        _fixture.DeclareQueue(TriggerFunctions.PocoTriggerQueueName);

        var testPoco = new TestMessage
        {
            Id = 42,
            Content = $"POCO content {Guid.NewGuid()}",
        };
        var jsonMessage = JsonSerializer.Serialize(testPoco);

        // Act
        _fixture.PublishMessage(TriggerFunctions.PocoTriggerQueueName, jsonMessage);

        // Assert
        var received = await _fixture.WaitForMessagesAsync(1, _defaultTimeout);
        Assert.True(received, "Timed out waiting for message");
        Assert.Single(_fixture.ReceivedMessages);
        Assert.Equal($"{testPoco.Id}:{testPoco.Content}", _fixture.ReceivedMessages[0]);
    }

    [Fact]
    public async Task BasicDeliverEventArgsTriggerReceivesRawMessage()
    {
        // Arrange
        _fixture.ClearReceivedMessages();
        _fixture.DeclareQueue(TriggerFunctions.BasicDeliverEventArgsTriggerQueueName);

        var testMessage = $"BasicDeliverEventArgs test {Guid.NewGuid()}";

        // Act
        _fixture.PublishMessage(TriggerFunctions.BasicDeliverEventArgsTriggerQueueName, testMessage);

        // Assert
        var received = await _fixture.WaitForMessagesAsync(1, _defaultTimeout);
        Assert.True(received, "Timed out waiting for message");
        Assert.Single(_fixture.ReceivedMessages);
        Assert.Equal(testMessage, _fixture.ReceivedMessages[0]);
    }

    [Fact]
    public async Task TriggerWithOutputBindingProcessesMessage()
    {
        // Arrange
        _fixture.ClearReceivedMessages();
        _fixture.DeclareQueue(OutputFunctions.TriggerOutputQueueName);
        _fixture.DeclareQueue(OutputFunctions.OutputResultQueueName);

        var originalMessage = $"Original {Guid.NewGuid()}";
        var expectedProcessedMessage = $"Processed: {originalMessage}";

        // Act
        _fixture.PublishMessage(OutputFunctions.TriggerOutputQueueName, originalMessage);

        // Assert
        var received = await _fixture.WaitForMessagesAsync(1, _defaultTimeout);
        Assert.True(received, "Timed out waiting for processed message");
        Assert.Single(_fixture.ReceivedMessages);
        Assert.Equal(expectedProcessedMessage, _fixture.ReceivedMessages[0]);
    }

    [Fact]
    public async Task MultipleMessagesProcessedInOrder()
    {
        // Arrange
        _fixture.ClearReceivedMessages();
        _fixture.DeclareQueue(TriggerFunctions.StringTriggerQueueName);

        var messages = new List<string>
        {
            $"Message 1 {Guid.NewGuid()}",
            $"Message 2 {Guid.NewGuid()}",
            $"Message 3 {Guid.NewGuid()}",
        };

        // Act
        foreach (var message in messages)
        {
            _fixture.PublishMessage(TriggerFunctions.StringTriggerQueueName, message);
        }

        // Assert
        var received = await _fixture.WaitForMessagesAsync(messages.Count, _defaultTimeout);
        Assert.True(received, "Timed out waiting for messages");
        Assert.Equal(messages.Count, _fixture.ReceivedMessages.Count);

        // Verify all messages were received (order may vary due to concurrent processing)
        foreach (var message in messages)
        {
            Assert.Contains(message, _fixture.ReceivedMessages);
        }
    }
}
