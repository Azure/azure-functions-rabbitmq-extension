// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests;

public class RabbitMQOptionsTests
{
    [Fact]
    public void TestDefaultOptions()
    {
        var options = new RabbitMQOptions();

        Assert.Null(options.ConnectionString);
        Assert.Null(options.QueueName);
        Assert.Equal<ushort>(30, options.PrefetchCount);
        Assert.False(options.DisableCertificateValidation);

        // Test formatted
        Assert.Equal(GetFormattedOption(options), options.Format());
    }

    [Fact]
    public void TestConfiguredRabbitMQOptions()
    {
        string expectedConnectionString = "someConnectionString";
        string expectedQueueName = "someQueueName";
        ushort expectedPrefetchCount = 100;
        bool expectedDisableCertificateValidation = true;
        string expectedSslCertPath = "someCertPath";
        string expectedSslCertPassphrase = "someCertPassphrase";
        string expectedSslCertThumbprint = "someCertThumbprint";

        var options = new RabbitMQOptions()
        {
            ConnectionString = expectedConnectionString,
            QueueName = expectedQueueName,
            PrefetchCount = expectedPrefetchCount,
            DisableCertificateValidation = expectedDisableCertificateValidation,
            SslCertPath = expectedSslCertPath,
            SslCertPassphrase = expectedSslCertPassphrase,
            SslCertThumbprint = expectedSslCertThumbprint,
        };

        Assert.Equal(expectedConnectionString, options.ConnectionString);
        Assert.Equal(expectedQueueName, options.QueueName);
        Assert.Equal(expectedPrefetchCount, options.PrefetchCount);
        Assert.Equal(expectedDisableCertificateValidation, options.DisableCertificateValidation);
        Assert.Equal(expectedSslCertPath, options.SslCertPath);
        Assert.Equal(expectedSslCertPassphrase, options.SslCertPassphrase);
        Assert.Equal(expectedSslCertThumbprint, options.SslCertThumbprint);

        // Test formatted
        Assert.Equal(GetFormattedOption(options), options.Format());
    }

    [Fact]
    public void TestJobHostHasTheRightConfiguration()
    {
        ushort expectedPrefetchCount = 10;

        IHostBuilder builder = new HostBuilder()
          .UseEnvironment("Development")
          .ConfigureWebJobs(webJobsBuilder =>
          {
              webJobsBuilder.AddRabbitMQ(a => a.PrefetchCount = expectedPrefetchCount); // set to non-default prefetch count
          })
          .UseConsoleLifetime();

        IHost host = builder.Build();
        using (host)
        {
            IOptions<RabbitMQOptions> config = host.Services.GetService<IOptions<RabbitMQOptions>>();
            Assert.Equal(config.Value.PrefetchCount, expectedPrefetchCount);
        }
    }

    // TODO: Do not immitate source code.
    private static string GetFormattedOption(RabbitMQOptions option)
    {
        var options = new JObject
        {
            { nameof(option.QueueName), option.QueueName },
            { nameof(option.PrefetchCount), option.PrefetchCount },
            { nameof(option.DisableCertificateValidation), option.DisableCertificateValidation },
            { nameof(option.SslCertPath), option.SslCertPath },
            { nameof(option.SslCertPassphrase), option.SslCertPassphrase },
            { nameof(option.SslCertThumbprint), option.SslCertThumbprint },
        };

        return options.ToString(Formatting.Indented);
    }
}
