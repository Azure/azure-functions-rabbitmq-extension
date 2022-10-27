# Release Process

> **Note:** The steps below are only meant to be followed by contributors having write permission to the repository and having access to Microsoft internal resources. The tag name `v2.0.0-preview` and the version number `2.0.0-preview` should be replaced to match with the package that is about to be released. The commands were run from PowerShell 7.2.5.

1. Choose the branch/commit as per your requirement. Create a tag and push it to GitHub repository. The tag should match patterns like `v2.0.0`, `v2.0.0-beta`, `v2.0.0-preview2`, etc. Tags like `2.0.0`, `v2.0` or `v2.0.0beta` will result in failed builds.

    ```console
    > git fetch origin
    > git tag v2.0.0-preview origin/dev
    > git push origin v2.0.0-preview
    ```

1. This should trigger a release build. The build progress can be viewed on [RabbitMQ Extension Build](https://dev.azure.com/azfunc/Azure%20Functions/_build?definitionId=48) page.

1. Ensure that the build completes successfully. Check that there are no warning messages and the build artifacts are created in below tree format. If you find any problems, please get them fixed first and restart the process.

    ```text
    /
    ├───drop-extension
    │   └───2.0.0-preview
    │       ├───Microsoft.Azure.WebJobs.Extensions.RabbitMQ.2.0.0-preview.nupkg
    │       ├───Microsoft.Azure.WebJobs.Extensions.RabbitMQ.2.0.0-preview.symbols.nupkg
    │       └───_manifest
    │           └───<files>
    └───drop-java-library
        └───2.0.0-preview
            ├───_manifest
            │   └───<files>
            ├───azure-functions-java-library-rabbitmq-2.0.0-preview-javadoc.jar
            ├───azure-functions-java-library-rabbitmq-2.0.0-preview-sources.jar
            ├───azure-functions-java-library-rabbitmq-2.0.0-preview.jar
            └───azure-functions-java-library-rabbitmq-2.0.0-preview.pom
    ```

1. Download the build artifacts, including the NuGet and JAR file.

1. Test the NuGet package.

    1. Add package to local NuGet feed.

        ```console
        > nuget sources Add -Name 'local' -Source 'C:\Source\nuget'
        > nuget add 'C:\Users\<user>\Downloads\drop-extension\2.0.0-preview\Microsoft.Azure.WebJobs.Extensions.RabbitMQ.2.0.0-preview.nupkg' -Source 'C:\Source\nuget'
        ```

    1. Update the package reference in your test application project such as in [this sample](https://github.com/JatinSanghvi/rabbitmq-functionapp).

        ```xml
        <PackageReference Include="Microsoft.Azure.WebJobs.Extensions.RabbitMQ" Version="2.0.0-preview" />
        ```

    1. Follow the steps in the sample repository's Readme file. Make sure that the assembly gets restored and the test application runs as expected.

1. Test the Java library.

    1. Add library files to local Maven repository.

        ```console
        > Copy-Item 'C:\Users\<user>\Downloads\drop-java-library\2.0.0-preview' 'C:\Users\<user>\.m2\repository\com\microsoft\azure\functions\azure-functions-java-library-rabbitmq\' -Recurse
        ```

    1. Update the package reference in your test application project such as in [this sample](https://github.com/JatinSanghvi/rabbitmq-java-functionapp).

        - **`pom.xml`**

            ```xml
            <dependency>
                <groupId>com.microsoft.azure.functions</groupId>
                <artifactId>azure-functions-java-library-rabbitmq</artifactId>
                <version>2.0.0-preview</version>
            </dependency>
            ```

        - **`extensions.csproj`**

            ```xml
            <PackageReference Include="Microsoft.Azure.WebJobs.Extensions.RabbitMQ" Version="2.0.0-preview" />
            ```

    1. Follow the steps in the sample repository's Readme file. Make sure that the assembly and java library gets restored and the test application runs as expected.

1. Perform cleanup. This will ensure that the tests to be run after publishing the packages will download the packages from public sources instead of copying them directly from the cache.

    ```console
    > Remove-Item 'C:\Users\<user>\.m2\repository\com\microsoft\azure\functions\azure-functions-java-library-rabbitmq\2.0.0-preview' -Recurse
    > Remove-Item 'C:\Source\nuget\microsoft.azure.webjobs.extensions.rabbitmq\2.0.0-preview' -Recurse
    > nuget sources Remove -Name 'local'
    ```

1. Create a new release using [RabbitMQ Extension Release](https://dev.azure.com/azfunc/Azure%20Functions/_release?definitionId=63) pipeline.

    1. Make sure that the `Version` input matches the title of the release build whose artifacts you have tested.
    1. The release will await manual approval before it can resume. The approvers are supposed to ensure that the build artifacts point to correct branch (e.g. `refs/tags/v2.0.0-preview`).

    The release pipeline has two stages: *NuGet Publish* and *Maven Publish*. Both of them leverage Azure SDK Partner Release Pipeline ([Wiki](https://dev.azure.com/azure-sdk/internal/_wiki/wikis/internal.wiki/1/Partner-Release-Pipeline)) to publish the packages to respective locations. Our release will trigger [net - partner-release](https://dev.azure.com/azure-sdk/internal/_build?definitionId=4442) pipeline in *NuGet Publish* stage and [java - partner-release](https://dev.azure.com/azure-sdk/internal/_build?definitionId=1809) pipeline in *Maven Publish* stage. Both stages will wait for the respective pipelines to complete successfully before proceeding. The stages should fail if the corresponding partner release pipeline has failed.

1. (Only if required) If the release fails, check the release logs to know if the failure is caused by expiration of the personal access token (PAT) used to initiate the partner release pipeline. If that is the case:

    1. Make sure you have access to run the partner pipelines. The instructions can be found in the Partner Release Pipeline Wiki.
    1. Open [Personal Access Tokens](https://dev.azure.com/azure-sdk/_usersSettings/tokens) page on Azure DevOps site for *azure-sdk* organization.
    1. Generate a new token having access to *azure-sdk* organization and with *Read, write, execute & manage Release* scope.
    1. Update the release pipeline's `Azure SDK Release PAT` variable value to the new token.

1. Ensure that the packages are published.

    1. NuGet package should be available on [NuGet Gallery](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.RabbitMQ).
    1. Maven artifacts should be available on [Nexus Repository Manager](https://oss.sonatype.org/#nexus-search;quick~azure-functions-java-library-rabbitmq).

    It may take few days for Maven artifacts to be listed on [MVN Repository](https://mvnrepository.com/artifact/com.microsoft.azure.functions/azure-functions-java-library-rabbitmq) and on [Maven Central Repository](https://search.maven.org/artifact/com.microsoft.azure.functions/azure-functions-java-library-rabbitmq) but that should not be a concern; the artifacts should be available for consumption.

1. Repeat the step to test the NuGet package, but with the package sourced from public NuGet gallery this time.
1. Repeat the step to test the Java library sourced from Maven Central this time.

1. Publish the release on GitHub.

    1. Open [Draft a new release](https://github.com/Azure/azure-functions-rabbitmq-extension/releases/new) page.
    1. Select the release tag.
    1. Set the release title same as the tag name.
    1. Click on the *Auto-generate release notes* to generate the bottom-section of the release description.
    1. For the top-section, provide release summary and enlist the improvements, bug fixes and breaking changes that went into the release.
    1. Try to maintain consistency with [v2.0.0-preview release notes](https://github.com/Azure/azure-functions-rabbitmq-extension/releases/tag/v2.0.0-preview).
    1. If it is a pre-release, tick the *This is a pre-release* checkbox before publishing the release.
