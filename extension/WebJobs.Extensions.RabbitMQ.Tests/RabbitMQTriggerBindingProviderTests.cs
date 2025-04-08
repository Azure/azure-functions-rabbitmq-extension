// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests;

public class RabbitMQTriggerBindingProviderTests
{
    [Fact]
    public async System.Threading.Tasks.Task Null_Context_Throws_Error()
    {
        IConfiguration emptyConfig = new ConfigurationBuilder().Build();
        var configProvider = new RabbitMQExtensionConfigProvider(
            Options.Create(new RabbitMQOptions()),
            new DefaultNameResolver(emptyConfig),
            new Mock<IRabbitMQServiceFactory>().Object,
            NullLoggerFactory.Instance,
            emptyConfig);
        var bindingProvider = new RabbitMQTriggerAttributeBindingProvider(
            new Mock<INameResolver>().Object,
            configProvider,
            NullLogger.Instance,
            Options.Create(new RabbitMQOptions()),
            emptyConfig);
        await Assert.ThrowsAsync<ArgumentNullException>(() => bindingProvider.TryCreateAsync(null));
    }
}
