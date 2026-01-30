// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.EndToEndTests;

/// <summary>
/// Test fixture that manages RabbitMQ container lifecycle using Testcontainers.
/// </summary>
public sealed class RabbitMQEndToEndTestFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private IHost _host;
    private IConnection _connection;
    private IModel _channel;

    public RabbitMQEndToEndTestFixture()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(5672))
            .Build();
    }

    /// <summary>
    /// Gets the RabbitMQ connection string for the test container.
    /// </summary>
    public string ConnectionString => _rabbitMqContainer.GetConnectionString();

    /// <summary>
    /// Gets the RabbitMQ channel for direct queue operations.
    /// </summary>
    public IModel Channel => _channel ?? throw new InvalidOperationException("Channel not initialized");

    /// <summary>
    /// Gets the WebJobs host for running functions.
    /// </summary>
    public IHost Host => _host ?? throw new InvalidOperationException("Host not initialized");

    /// <summary>
    /// Gets the received messages from the trigger test functions.
    /// </summary>
    public List<string> ReceivedMessages { get; } = new List<string>();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Start RabbitMQ container
        await _rabbitMqContainer.StartAsync();

        // Create RabbitMQ connection and channel
        var factory = new ConnectionFactory
        {
            Uri = new Uri(ConnectionString),
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Pre-create all queues used by trigger functions before starting the host
        // This is required because the RabbitMQ extension validates queue existence on startup
        _channel.QueueDeclare(queue: TriggerFunctions.StringTriggerQueueName, durable: false, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: TriggerFunctions.ByteArrayTriggerQueueName, durable: false, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: TriggerFunctions.PocoTriggerQueueName, durable: false, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: TriggerFunctions.BasicDeliverEventArgsTriggerQueueName, durable: false, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: OutputFunctions.TriggerOutputQueueName, durable: false, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: OutputFunctions.OutputResultQueueName, durable: false, exclusive: false, autoDelete: false);

        // Build and start the WebJobs host
        var builder = new HostBuilder()
            .ConfigureWebJobs(webJobsBuilder =>
            {
                webJobsBuilder.AddRabbitMQ();
            })
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["RabbitMQConnection"] = ConnectionString,
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(this);
            });

        _host = builder.Build();
        await _host.StartAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        _channel?.Dispose();
        _connection?.Dispose();

        await _rabbitMqContainer.DisposeAsync();
    }

    /// <summary>
    /// Declares a queue for testing.
    /// </summary>
    /// <param name="queueName">The name of the queue to declare.</param>
    public void DeclareQueue(string queueName)
    {
        Channel.QueueDeclare(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    /// <summary>
    /// Publishes a message to the specified queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="message">The message body.</param>
    public void PublishMessage(string queueName, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        Channel.BasicPublish(
            exchange: string.Empty,
            routingKey: queueName,
            basicProperties: null,
            body: body);
    }

    /// <summary>
    /// Publishes a message with headers to the specified queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="message">The message body.</param>
    /// <param name="headers">The message headers.</param>
    public void PublishMessageWithHeaders(string queueName, string message, IDictionary<string, object> headers)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var properties = Channel.CreateBasicProperties();
        properties.Headers = headers;
        Channel.BasicPublish(
            exchange: string.Empty,
            routingKey: queueName,
            basicProperties: properties,
            body: body);
    }

    /// <summary>
    /// Clears all received messages.
    /// </summary>
    public void ClearReceivedMessages()
    {
        ReceivedMessages.Clear();
    }

    /// <summary>
    /// Waits for the specified number of messages to be received.
    /// </summary>
    /// <param name="expectedCount">The expected number of messages.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>True if the expected number of messages was received within the timeout.</returns>
    public async Task<bool> WaitForMessagesAsync(int expectedCount, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        while (ReceivedMessages.Count < expectedCount)
        {
            if (DateTime.UtcNow - startTime > timeout)
            {
                return false;
            }

            await Task.Delay(100);
        }

        return true;
    }
}

/// <summary>
/// Collection definition for RabbitMQ E2E tests.
/// </summary>
[CollectionDefinition(RabbitMQE2ETestFixtureDefinition.Name)]
public class RabbitMQE2ETestFixtureDefinition : ICollectionFixture<RabbitMQEndToEndTestFixture>
{
    public const string Name = "RabbitMQ E2E Tests";
}
