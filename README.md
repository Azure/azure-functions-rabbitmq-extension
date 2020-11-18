|Branch|Status|
|---|---|
|master|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/azure-functions-rabbitmq-extension-ci?branchName=master)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=34&branchName=master)|
|dev|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/azure-functions-rabbitmq-extension-ci?branchName=dev)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=34&branchName=dev)|
<<<<<<< HEAD

NuGet Package [Microsoft.Azure.WebJobs.Extensions.RabbitMQ](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.RabbitMQ)

=======
NuGet Package [Microsoft.Azure.WebJobs.Extensions.RabbitMQ](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.RabbitMQ)
>>>>>>> 5a993e9dc6fe3c9067c9eb47e0314ef7e4ff268a
# RabbitMQ Binding Support for Azure Functions

The Azure Functions RabbitMQ Binding extensions allows you to send and receive messages using the RabbitMQ API but by writing Functions code. The RabbitMQ output binding sends messages to a specific queue. The RabbitMQ trigger fires when it receives a message from a specific queue.

[RabbitMQ Documentation for the .NET Client](https://www.rabbitmq.com/dotnet-api-guide.html)

To get started with developing with this extension, make sure you first [set up a RabbitMQ endpoint](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki/Setting-up-a-RabbitMQ-Endpoint). Then you can go ahead and begin developing your functions in [C#](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki/Samples-in-C%23), [JavaScript](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki/Samples-in-JavaScript), or [Python](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki/Samples-in-Python). If you would like a way to handle messages that error, check out our [guide to configuring a dead letter exchange](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki/Configuring-a-Dead-Letter-Exchange-and-Queue).

# Samples

See the repository [wiki](https://github.com/Azure/azure-functions-rabbitmq-extension/wiki) for more detailed samples of bindings to different types.

```C#
public static void RabbitMQTrigger_RabbitMQOutput(
    [RabbitMQTrigger("queue", ConnectionStringSetting = "RabbitMQConnection")] string inputMessage,
    [RabbitMQ(
        ConnectionStringSetting = "RabbitMQConnection",
        QueueName = "hello")] out string outputMessage,
    ILogger logger)
{
    outputMessage = inputMessage;
    logger.LogInformation($"RabittMQ output binding function sent message: {outputMessage}");
}
```

The above sample waits on a trigger from the queue named "queue" connected to the connection string value of key "RabbitMQConnection." The output binding takes the messages from the trigger queue and outputs them to queue "hello" connected to the connection configured by the key "RabibtMQConnection". When running locally, add the connection string setting to local.settings.json file. When running in Azure, add this setting as [Application Setting](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings) for your app.


## Properties

|Property Name|Description|Example|
|--|--|--|
|ConnectionStringSetting|The connection string for the RabbitMQ queue|`amqp://user:password@url:port`|
|QueueName|The name of the source or destination queue. To move failed messages to deadletter queue, please configure queue and exchange(https://www.rabbitmq.com/dlx.html)|`myQueue`|
|HostName|(optional if using ConnectionStringSetting) Hostname of the queue|`10.26.45.210`|
|UserName|(optional if using ConnectionStringSetting) User name to access queue|`user`|
|Password|(optional if using ConnectionStringSetting) Password to access queue|`password1`|

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
