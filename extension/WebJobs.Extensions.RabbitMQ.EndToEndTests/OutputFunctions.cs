// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.EndToEndTests;

/// <summary>
/// Test functions for RabbitMQ output binding tests.
/// </summary>
public class OutputFunctions
{
    public const string OutputQueueName = "e2e-output-queue";
    public const string TriggerOutputQueueName = "e2e-trigger-output-queue";
    public const string OutputResultQueueName = "e2e-output-result-queue";

    private readonly RabbitMQEndToEndTestFixture _fixture;
    private readonly ILogger<OutputFunctions> _logger;

    public OutputFunctions(RabbitMQEndToEndTestFixture fixture, ILogger<OutputFunctions> logger)
    {
        _fixture = fixture;
        _logger = logger;
    }

    /// <summary>
    /// Trigger function that receives a message and outputs to another queue.
    /// </summary>
    [FunctionName(nameof(TriggerWithOutputBinding))]
    [return: RabbitMQ(QueueName = OutputResultQueueName, ConnectionStringSetting = "RabbitMQConnection")]
    public string TriggerWithOutputBinding(
        [RabbitMQTrigger(TriggerOutputQueueName, ConnectionStringSetting = "RabbitMQConnection")] string message)
    {
        _logger.LogInformation("TriggerWithOutputBinding received: {Message}", message);
        var processedMessage = $"Processed: {message}";
        return processedMessage;
    }

    /// <summary>
    /// Collects output messages from the result queue.
    /// </summary>
    [FunctionName(nameof(OutputResultCollector))]
    public void OutputResultCollector(
        [RabbitMQTrigger(OutputResultQueueName, ConnectionStringSetting = "RabbitMQConnection")] string message)
    {
        _logger.LogInformation("OutputResultCollector received: {Message}", message);
        _fixture.ReceivedMessages.Add(message);
    }
}
