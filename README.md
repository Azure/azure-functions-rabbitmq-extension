# RabbitMQ Extension for Azure Functions

[![Build Status](https://azfunc.visualstudio.com/public/_apis/build/status%2Fazure%2Fazure-functions-rabbitmq-extension%2Frabbitmq-extension-linux.public?repoName=Azure%2Fazure-functions-rabbitmq-extension&branchName=dev)](https://azfunc.visualstudio.com/public/_build/latest?definitionId=807&repoName=Azure%2Fazure-functions-rabbitmq-extension&branchName=dev)

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

## Package feed

All Maven packages and plugins are restored from the `upstream-public` Azure Artifacts feed
(`https://pkgs.dev.azure.com/azfunc/public/_packaging/upstream-public/maven/v1`), which is configured
as the `central` repository in `java-library/pom.xml` and in the Java end-to-end test app. NuGet
restores already go through the same feed via [`NuGet.config`](NuGet.config).

The repository root also has a [`settings.xml`](settings.xml) that mirrors every remote repository
(`external:*`) to the same feed. It exists because a `pom.xml` cannot cover everything:

- Maven resolves build extensions and plugin prefixes *before* a pom's `<repositories>` are honored,
  so those requests would otherwise go straight to Maven Central.
- `MavenAuthenticate@0` and the credential provider key credentials off the Azure Artifacts *feed
  name* (`upstream-public`), while the pom repository id must be `central` in order to override the
  id Maven inherits from the Super POM. The mirror id bridges the two.
- `java-library/pom.xml` inherits `java-8-parent`, which contributes a Sonatype snapshot plugin
  repository this repository does not own. Only the mirror can keep that traffic on the feed.

CI installs this file to `~/.m2/settings.xml`. Locally you only need it when pulling a package or
version the feed has not cached yet, in which case pass it explicitly with `mvn -s settings.xml`.

The end-to-end test image does not need it. Its `pom.xml` declares the feed as `central`, which
covers dependency and plugin resolution inside the container.

### Anonymous restore (default)

The feed allows anonymous reads, so no credentials are required to build once a package version has
been saved to the feed. External contributors and fresh clones need no setup. `mvn` just works.
Never commit credentials or a `<server>` entry to `settings.xml` in this repository because doing so
would force authentication on everyone.

### Authenticating (Microsoft developers only)

Authentication is only needed to *ingest* a package version that the feed has not cached yet. The
first restore of any new or upgraded dependency will fail anonymously with:

> No local versions of package '...'; please provide authentication to access versions from upstream
> that have not yet been saved to your feed.

When that happens, a Microsoft developer with access to the `azfunc/public` project must run the
restore once with credentials, which pulls the version from upstream and saves it to the feed. Every
subsequent anonymous restore then succeeds.

The recommended way to authenticate is the `artifacts-maven-credprovider`, which acquires a token via
Entra ID so you do not have to manage a PAT.

Run the helper script for your shell from the root of your clone. It installs the credential provider
into your local Maven repository if it is missing, then writes `.mvn/extensions.xml`. Both scripts
are idempotent, so re-running them is safe:

```powershell
./eng/scripts/Install-MavenCredentialProvider.ps1
```

```bash
./eng/scripts/install-maven-credprovider.sh
```

Pass `-Version` / `--version` to install a different release, and `-Force` / `--force` to reinstall or
to overwrite an `.mvn/extensions.xml` the script does not manage.

`.mvn/` is deliberately listed in `.gitignore`. Do not commit it. The extension exits when it detects
a build context, and committing it would break anonymous restores for everyone else.

If you would rather not use the credential provider, you can instead add a `<server>` entry to your
user-level `~/.m2/settings.xml` (never to a file inside this repository), using an Azure DevOps
personal access token with Packaging read and write scope:

```xml
<settings>
  <servers>
    <server>
      <!-- Must match the <id> of the repository declared in the pom.xml files. -->
      <id>central</id>
      <username>azfunc</username>
      <password>[PERSONAL_ACCESS_TOKEN]</password>
    </server>
  </servers>
</settings>
```

CI covers this automatically. The `MavenAuthenticate@0` task in the build templates authenticates the
feed, so merged changes to dependency versions are ingested by the pipeline. The credential provider
is not used in pipelines.

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repositories using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
