// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests;

public class UtilityTests
{
    private readonly IConfiguration emptyConfig = new ConfigurationBuilder().AddJsonFile("testappsettings.json").Build();

    [Theory]
    [InlineData("", "hello", "hello")]
    [InlineData("rabbitMQTest", "hello", "amqp://guest:guest@tada:5672")]
    public void ResolveConnectionString(string attributeConnectionString, string optionsConnectionString, string expectedResolvedString)
    {
        string resolvedString = Utility.ResolveConnectionString(attributeConnectionString, optionsConnectionString, this.emptyConfig);
        Assert.Equal(expectedResolvedString, resolvedString);
    }
}
