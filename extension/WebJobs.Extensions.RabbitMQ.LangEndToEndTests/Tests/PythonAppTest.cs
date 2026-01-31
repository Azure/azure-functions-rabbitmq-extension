// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.LangEndToEndTests.Tests;

/// <summary>
/// E2E tests for Python Function App with RabbitMQ bindings.
/// </summary>
[Collection("RabbitMQ Lang E2E")]
public class PythonAppTest : BaseE2E
{
    public PythonAppTest(RabbitMQE2EFixture fixture) : base(fixture)
    {
    }

    [Fact(Skip = "Requires Docker Compose environment")]
    public async Task Python_HttpTriggerRabbitMQOutput_SendsMessage()
    {
        // Arrange
        var testMessage = GenerateTestMessage("python-http");
        await ClearQueueAsync(Constants.Queues.ResultQueue);

        // Act - Send message via HTTP trigger
        var response = await SendMessageViaHttpAsync(
            Constants.FunctionApps.Python.HttpTriggerRabbitMQOutput,
            testMessage);

        // Assert - HTTP response should be OK
        Assert.True(response.IsSuccessStatusCode, $"HTTP request failed: {response.StatusCode}");

        // Assert - Message should be received by RabbitMQ trigger and forwarded to Storage Queue
        var receivedMessage = await WaitForQueueMessageAsync(
            Constants.Queues.ResultQueue,
            testMessage,
            Constants.Timeouts.MessageWait);

        Assert.NotNull(receivedMessage);
        Assert.Contains(testMessage, receivedMessage);
    }

    [Fact(Skip = "Requires Docker Compose environment")]
    public async Task Python_RabbitMQTrigger_ProcessesMultipleMessages()
    {
        // Arrange
        var messages = Enumerable.Range(1, 5)
            .Select(i => GenerateTestMessage($"python-multi-{i}"))
            .ToList();
        await ClearQueueAsync(Constants.Queues.ResultQueue);

        // Act - Send multiple messages
        foreach (var message in messages)
        {
            var response = await SendMessageViaHttpAsync(
                Constants.FunctionApps.Python.HttpTriggerRabbitMQOutput,
                message);
            Assert.True(response.IsSuccessStatusCode);
        }

        // Assert - All messages should be processed
        foreach (var expectedMessage in messages)
        {
            var receivedMessage = await WaitForQueueMessageAsync(
                Constants.Queues.ResultQueue,
                expectedMessage,
                Constants.Timeouts.MessageWait);

            Assert.NotNull(receivedMessage);
        }
    }
}
