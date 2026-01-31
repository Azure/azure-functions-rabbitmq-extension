// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Storage.Queues;
using RabbitMQ.Client;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.LangEndToEndTests.Tests;

/// <summary>
/// Infrastructure tests to verify RabbitMQ and Azurite connectivity.
/// </summary>
public class InfrastructureTest
{
    private readonly ITestOutputHelper _output;

    public InfrastructureTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RabbitMQ_CanConnect()
    {
        // Arrange
        var factory = new ConnectionFactory
        {
            HostName = Constants.RabbitMQ.Host,
            Port = Constants.RabbitMQ.Port,
            UserName = Constants.RabbitMQ.User,
            Password = Constants.RabbitMQ.Password
        };

        // Act & Assert
        using var connection = await factory.CreateConnectionAsync();
        Assert.True(connection.IsOpen);
        _output.WriteLine($"Connected to RabbitMQ at {Constants.RabbitMQ.Host}:{Constants.RabbitMQ.Port}");

        // Create a test queue
        using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(
            queue: "test-connectivity-queue",
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null);
        _output.WriteLine("Successfully created test queue");
    }

    [Fact]
    public async Task Azurite_CanConnect()
    {
        // Arrange
        var queueServiceClient = new QueueServiceClient(Constants.Azurite.ConnectionString);

        // Act
        var properties = await queueServiceClient.GetPropertiesAsync();

        // Assert
        Assert.NotNull(properties);
        _output.WriteLine($"Connected to Azurite at {Constants.Azurite.Host}:{Constants.Azurite.QueuePort}");

        // Create a test queue
        var queueClient = queueServiceClient.GetQueueClient("test-connectivity-queue");
        await queueClient.CreateIfNotExistsAsync();
        _output.WriteLine("Successfully created test queue in Azurite");

        // Clean up
        await queueClient.DeleteIfExistsAsync();
    }

    [Fact]
    public async Task RabbitMQ_CanSendAndReceiveMessage()
    {
        // Arrange
        var factory = new ConnectionFactory
        {
            HostName = Constants.RabbitMQ.Host,
            Port = Constants.RabbitMQ.Port,
            UserName = Constants.RabbitMQ.User,
            Password = Constants.RabbitMQ.Password
        };

        var testMessage = $"test-message-{Guid.NewGuid()}";
        var queueName = "test-send-receive-queue";

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Declare queue
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null);

        // Act - Send message
        var body = System.Text.Encoding.UTF8.GetBytes(testMessage);
        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            body: body);
        _output.WriteLine($"Sent message: {testMessage}");

        // Act - Receive message
        var result = await channel.BasicGetAsync(queueName, autoAck: true);

        // Assert
        Assert.NotNull(result);
        var receivedMessage = System.Text.Encoding.UTF8.GetString(result.Body.ToArray());
        Assert.Equal(testMessage, receivedMessage);
        _output.WriteLine($"Received message: {receivedMessage}");
    }
}
