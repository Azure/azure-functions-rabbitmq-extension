"""
RabbitMQ E2E Test Functions for Python
Uses the v2 programming model with decorators
"""

import azure.functions as func
import logging

app = func.FunctionApp()


@app.function_name("HttpTriggerRabbitMQOutput")
@app.route(route="HttpTriggerRabbitMQOutput", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
@app.generic_output_binding(
    arg_name="rabbitmqOutput",
    type="rabbitMQ",
    connection_string_setting="RabbitMQConnectionString",
    queue_name="test-input-queue"
)
def http_trigger_rabbitmq_output(req: func.HttpRequest, rabbitmqOutput: func.Out[str]) -> func.HttpResponse:
    """
    HTTP Trigger that publishes message to RabbitMQ queue.
    """
    message = req.get_body().decode('utf-8')
    logging.info(f'Received HTTP request with message: {message}')

    # Send message to RabbitMQ
    rabbitmqOutput.set(message)

    return func.HttpResponse(
        f"Message sent to RabbitMQ: {message}",
        status_code=200
    )


@app.function_name("RabbitMQTriggerQueueOutput")
@app.generic_trigger(
    arg_name="message",
    type="rabbitMQTrigger",
    connection_string_setting="RabbitMQConnectionString",
    queue_name="test-input-queue"
)
@app.queue_output(
    arg_name="queueOutput",
    queue_name="test-result-queue",
    connection="AzureWebJobsStorage"
)
def rabbitmq_trigger_queue_output(message: str, queueOutput: func.Out[str]) -> None:
    """
    RabbitMQ Trigger that writes received message to Azure Storage Queue.
    """
    logging.info(f'RabbitMQ trigger received: {message}')

    # Forward message to Azure Storage Queue for verification
    queueOutput.set(message)

    logging.info('Message forwarded to Storage Queue')
