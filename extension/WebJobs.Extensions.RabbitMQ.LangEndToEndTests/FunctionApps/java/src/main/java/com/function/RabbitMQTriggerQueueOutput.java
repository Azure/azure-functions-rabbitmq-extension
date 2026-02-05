package com.function;

import com.microsoft.azure.functions.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.rabbitmq.annotation.*;

/**
 * RabbitMQ Trigger that writes received message to Azure Storage Queue.
 */
public class RabbitMQTriggerQueueOutput {
    @FunctionName("RabbitMQTriggerQueueOutput")
    public void run(
            @RabbitMQTrigger(
                connectionStringSetting = "RabbitMQConnectionString",
                queueName = "test-input-queue")
            String message,
            @QueueOutput(
                name = "output",
                queueName = "test-result-queue",
                connection = "AzureWebJobsStorage")
            OutputBinding<String> output,
            final ExecutionContext context) {

        context.getLogger().info("RabbitMQ trigger received: " + message);

        // Forward message to Azure Storage Queue for verification
        output.setValue(message);

        context.getLogger().info("Message forwarded to Storage Queue");
    }
}
