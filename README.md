# RabbitMQ Binding Support for Azure Functions

The Azure Functions RabbitMQ Binding extensions allows you to send and receive messages using the RabbitMQ API but by writing Functions code. The RabbitMQ output binding sends messages to a specific queue. The RabbitMQ trigger fires when it receives a message from a specific queue.

[RabbitMQ Documentation for the .NET Client](https://www.rabbitmq.com/dotnet-api-guide.html)

# Samples

See the repository [wiki](https://github.com/katiecai/azure-functions-rabbitmq-extension/wiki) for more detailed samples of bindings to different types.

## Output Binding

```C#
using Microsoft.Azure.WebJobs;
using RabbitMQ.Client;

public static void TimerTrigger_StringOutput(
    [TimerTrigger("00:01")] TimerInfo timer,
    [RabbitMQ(
        Hostname = "localhost",
        QueueName = "queue")] out string outputMessage)
{
    outputMessage = "hello"
}
```

The above example waits on a timer trigger to fire (every second) before sending a message to the queue named "queue" connected to the localhost port. The message we want to send is then bound to the variable outputMessage.

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
