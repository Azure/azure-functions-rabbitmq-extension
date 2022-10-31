# RabbitMQ Extension for Azure Functions

[![Build Status](https://dev.azure.com/azfunc/Azure%20Functions/_apis/build/status/Azure.azure-functions-rabbitmq-extension?branchName=dev)](https://dev.azure.com/azfunc/Azure%20Functions/_build/latest?definitionId=48&branchName=dev)

This repository hosts RabbitMQ trigger and output bindings to interact with RabbitMQ in your [Azure Functions](https://azure.microsoft.com/services/functions/)
and [WebJobs](https://learn.microsoft.com/azure/app-service/webjobs-sdk-how-to). More specifically, the trigger binding enables invoking a function when a message arrives at the RabbitMQ queue. The triggered function can consume this message and take required action. Similarly, the output binding facilitates publishing of messages on the RabbitMQ queue.

## Usage

The following example shows a [C# function](https://learn.microsoft.com/azure/azure-functions/functions-dotnet-class-library) that gets invoked (by virtue of the trigger binding) when a message is added to a RabbitMQ queue named `inputQueue`. The function then logs the message string, composes an output message and returns it. This value is then published to the queue named `outputQueue` through the output binding. The example function dictates that the connection URI for the RabbitMQ service is the one with key `RabbitMqConnectionString` in the [Application Settings](https://learn.microsoft.com/azure/azure-functions/functions-develop-local#local-settings-file).

```cs
[FunctionName("RabbitMqExample")]
[return: RabbitMQ(QueueName = "outputQueue", ConnectionStringSetting = "RabbitMqConnectionString")]
public static string Run(
    [RabbitMQTrigger(queueName: "inputQueue" ConnectionStringSetting = "RabbitMqConnectionString")] string name,
    ILogger logger)
{
    logger.LogInformation($"Message received: {name}.");
    return $"Hello, {name}.";
}
```

Along with `string` type, the extension also allows binding to the input arguments and returned values of `byte[]` type, POCO objects, and `BasicDeliverEventArgs` type. The last type is particularly useful for fetching of RabbitMQ message headers and other message properties. See the [repository wiki](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki) for detailed samples of bindings to different types.

## Getting Started

Before working with the RabbitMQ extension, you must [set up your RabbitMQ endpoint](https://www.rabbitmq.com/download.html). Then you can get started by following the sample functions in [C#](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki/Samples-in-C%23), [C# Script](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki/Samples-in-CSX), [JavaScript](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki/Samples-in-JavaScript), [Python](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki/Samples-in-Python) or [Java](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki/Samples-in-Java).

To learn about creating an application that works with RabbitMQ, see the [getting started](https://www.rabbitmq.com/getstarted.html) page. For general documentation on .NET RabbitMQ client usage, see the [.NET/C# client API guide](https://www.rabbitmq.com/dotnet-api-guide.html).

## C# Attributes

The following C# attributes are common to both RabbitMQ trigger and output bindings.

| Attribute Name | Type | Description |
|---|---|---|
| `ConnectionStringSetting` | `string` | The setting name for RabbitMQ connection URI. An example setting value would be `amqp://user:pass@host:10000/vhost`. |
| `DisableCertificateValidation` | `bool` | Indicates whether certificate validation should be disabled. Not recommended for production. Does not apply when SSL is disabled. |
| `QueueName` | `string` | The RabbitMQ queue name. |

## Java Annotations

The following Java annotations are common to both RabbitMQ trigger and output bindings.

| Annotation Name | Type | Description |
|---|---|---|
| `connectionStringSetting` | `String` | The setting name for RabbitMQ connection URI. An example setting value would be `amqp://user:pass@host:10000/vhost`. |
| `dataType` | `String` | Defines how the Functions runtime should treat the parameter value. Possible values are `""`, `"string"` and `"binary"`. |
| `disableCertificateValidation` | `boolean` | Indicates whether certificate validation should be disabled. Not recommended for production. Does not apply when SSL is disabled. |
| `queueName` | `String` | The RabbitMQ queue name. |

## Further Reading

Please refer to the Microsoft Docs page on [RabbitMQ bindings for Azure Functions overview](https://learn.microsoft.com/azure/azure-functions/functions-bindings-rabbitmq). It contains install instructions for all the supported programming languages, information on setting up and configuring the function app, and the  list of Azure App Service plans that support hosting of the function apps with RabbitMQ bindings.

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repositories using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
