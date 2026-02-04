// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.LangEndToEndTests;

/// <summary>
/// Base class for Lang E2E tests providing common utilities.
/// </summary>
[Collection("RabbitMQ Lang E2E")]
public abstract class BaseE2E
{
    protected RabbitMQE2EFixture Fixture { get; }
    protected HttpClient HttpClient => Fixture.HttpClient;
    protected QueueServiceClient QueueServiceClient => Fixture.QueueServiceClient;

    protected BaseE2E(RabbitMQE2EFixture fixture)
    {
        Fixture = fixture;
    }

    /// <summary>
    /// Sends a message via HTTP trigger to a function app.
    /// </summary>
    protected async Task<HttpResponseMessage> SendMessageViaHttpAsync(string endpoint, string message)
    {
        using var content = new StringContent(message, Encoding.UTF8, "application/json");
        return await HttpClient.PostAsync(endpoint, content);
    }

    /// <summary>
    /// Waits for a message containing the expected content in the Azure Storage Queue.
    /// </summary>
    protected async Task<string?> WaitForQueueMessageAsync(string queueName, string expectedContent, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? Constants.Timeouts.MessageWait;
        var stopwatch = Stopwatch.StartNew();

        var queueClient = QueueServiceClient.GetQueueClient(queueName);
        await queueClient.CreateIfNotExistsAsync();

        while (stopwatch.Elapsed < effectiveTimeout)
        {
            QueueMessage[] messages = await queueClient.ReceiveMessagesAsync(maxMessages: 10);

            foreach (var message in messages)
            {
                var body = message.Body.ToString();

                // Try base64 decode if needed
                try
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(body));
                    body = decoded;
                }
                catch
                {
                    // Not base64 encoded, use as-is
                }

                if (body.Contains(expectedContent))
                {
                    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                    return body;
                }
            }

            await Task.Delay(500);
        }

        return null;
    }

    /// <summary>
    /// Clears all messages from a queue.
    /// </summary>
    protected async Task ClearQueueAsync(string queueName)
    {
        var queueClient = QueueServiceClient.GetQueueClient(queueName);
        if (await queueClient.ExistsAsync())
        {
            await queueClient.ClearMessagesAsync();
        }
    }

    /// <summary>
    /// Generates a unique test message with a prefix.
    /// </summary>
    protected static string GenerateTestMessage(string prefix = "test")
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }
}
