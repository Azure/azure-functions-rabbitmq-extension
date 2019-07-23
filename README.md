# RabbitMQ Binding Support for Azure Functions

The Azure Functions RabbitMQ Binding extensions allows you to send and receive messages using the RabbitMQ API but by writing Functions code. The RabbitMQ output binding sends messages to a specific queue. The RabbitMQ trigger fires when it receives a message from a specific queue.

[RabbitMQ Documentation for the .NET Client](https://www.rabbitmq.com/dotnet-api-guide.html)

# Samples

See the repository [wiki](https://github.com/katiecai/azure-functions-rabbitmq-extension/wiki) for more detailed samples of bindings to different types.

## Output Binding

```C#
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public static void TimerTrigger_RabbitMQOutput(
    [TimerTrigger("00:01")] TimerInfo timer,
    [RabbitMQ(
        Hostname = "localhost",
        QueueName = "queue",
        Message = "Hello there")] out string outputMessage)
{
     var factory = new ConnectionFactory() { HostName = "localhost" };
    using (var connection = factory.CreateConnection())
    using (var channel = connection.CreateModel())
    {
        channel.QueueDeclare(queue: "queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        var receivedMessage = string.Empty;
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body;
            receivedMessage = Encoding.UTF8.GetString(body);
             Console.WriteLine("Received {0}", receivedMessage);
        };

        channel.BasicConsume(queue: "queue", autoAck: true, consumer: consumer);

        outputMessage = receivedMessage;
        Console.WriteLine(outputMessage);
    }
}
```

The above example waits on a timer trigger to fire (every second) before sending a message to the queue named "queue" connected to the localhost port. The body of the function creates a consumer with a queue of the same name, which receives the message shortly after it's sent by the output binding. The message is then bound to the variable outputMessage.

## Trigger Binding

```C#
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public static void RabbitMQTrigger_String(
    [RabbitMQTrigger("localhost", "queue")] string message,
    ILogger logger
    )
{
    logger.LogInformation($"RabbitMQ queue trigger function processed message: {message}");
}
```

This function is triggered on a message from the queue "queue" connected to the localhost port. The received message is bound to the variable message and is processed by the function.

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
