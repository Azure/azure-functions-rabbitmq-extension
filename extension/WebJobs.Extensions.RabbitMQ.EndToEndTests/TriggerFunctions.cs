// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.EndToEndTests;

/// <summary>
/// Test functions for RabbitMQ trigger binding tests.
/// </summary>
public class TriggerFunctions
{
    public const string StringTriggerQueueName = "e2e-string-trigger-queue";
    public const string ByteArrayTriggerQueueName = "e2e-bytearray-trigger-queue";
    public const string PocoTriggerQueueName = "e2e-poco-trigger-queue";
    public const string BasicDeliverEventArgsTriggerQueueName = "e2e-basiceventargs-trigger-queue";

    private readonly RabbitMQEndToEndTestFixture _fixture;
    private readonly ILogger<TriggerFunctions> _logger;

    public TriggerFunctions(RabbitMQEndToEndTestFixture fixture, ILogger<TriggerFunctions> logger)
    {
        _fixture = fixture;
        _logger = logger;
    }

    /// <summary>
    /// Trigger function that receives a string message.
    /// </summary>
    [FunctionName(nameof(StringTriggerFunction))]
    public void StringTriggerFunction(
        [RabbitMQTrigger(StringTriggerQueueName, ConnectionStringSetting = "RabbitMQConnection")] string message)
    {
        _logger.LogInformation("StringTriggerFunction received: {Message}", message);
        _fixture.ReceivedMessages.Add(message);
    }

    /// <summary>
    /// Trigger function that receives a byte array message.
    /// </summary>
    [FunctionName(nameof(ByteArrayTriggerFunction))]
    public void ByteArrayTriggerFunction(
        [RabbitMQTrigger(ByteArrayTriggerQueueName, ConnectionStringSetting = "RabbitMQConnection")] byte[] message)
    {
        var messageString = Encoding.UTF8.GetString(message);
        _logger.LogInformation("ByteArrayTriggerFunction received: {Message}", messageString);
        _fixture.ReceivedMessages.Add(messageString);
    }

    /// <summary>
    /// Trigger function that receives a POCO message.
    /// </summary>
    [FunctionName(nameof(PocoTriggerFunction))]
    public void PocoTriggerFunction(
        [RabbitMQTrigger(PocoTriggerQueueName, ConnectionStringSetting = "RabbitMQConnection")] TestMessage message)
    {
        _logger.LogInformation("PocoTriggerFunction received: Id={Id}, Content={Content}", message.Id, message.Content);
        _fixture.ReceivedMessages.Add($"{message.Id}:{message.Content}");
    }

    /// <summary>
    /// Trigger function that receives a BasicDeliverEventArgs message.
    /// </summary>
    [FunctionName(nameof(BasicDeliverEventArgsTriggerFunction))]
    public void BasicDeliverEventArgsTriggerFunction(
        [RabbitMQTrigger(BasicDeliverEventArgsTriggerQueueName, ConnectionStringSetting = "RabbitMQConnection")] BasicDeliverEventArgs eventArgs)
    {
        var messageString = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        _logger.LogInformation("BasicDeliverEventArgsTriggerFunction received: {Message}", messageString);
        _fixture.ReceivedMessages.Add(messageString);
    }
}

/// <summary>
/// Test POCO class for serialization tests.
/// </summary>
public class TestMessage
{
    public int Id { get; set; }

    public string Content { get; set; } = string.Empty;
}
