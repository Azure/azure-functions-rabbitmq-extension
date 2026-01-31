package com.function;

import com.microsoft.azure.functions.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.rabbitmq.annotation.*;

/**
 * HTTP Trigger that publishes message to RabbitMQ queue.
 */
public class HttpTriggerRabbitMQOutput {
    @FunctionName("HttpTriggerRabbitMQOutput")
    public HttpResponseMessage run(
            @HttpTrigger(
                name = "req",
                methods = {HttpMethod.POST},
                authLevel = AuthorizationLevel.ANONYMOUS)
            HttpRequestMessage<String> request,
            @RabbitMQOutput(
                connectionStringSetting = "RabbitMQConnectionString",
                queueName = "test-input-queue")
            OutputBinding<String> output,
            final ExecutionContext context) {

        String message = request.getBody();
        context.getLogger().info("Received message: " + message);

        // Send message to RabbitMQ
        output.setValue(message);

        return request.createResponseBuilder(HttpStatus.OK)
                .body("Message sent to RabbitMQ: " + message)
                .build();
    }
}
