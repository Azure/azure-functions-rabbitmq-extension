// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQOptionsTests
    {
        private string GetFormattedOption(RabbitMQOptions option)
        {
            JObject options = new JObject
            {
                { nameof(option.HostName), option.HostName },
                { nameof(option.QueueName), option.QueueName },
                { nameof(option.Port), option.Port },
                { nameof(option.PrefetchCount), option.PrefetchCount },
            };

            return options.ToString(Formatting.Indented);
        }

        [Fact]
        public void TestDefaultOptions()
        {
            RabbitMQOptions options = new RabbitMQOptions();

            Assert.Equal<ushort>(30, options.PrefetchCount);
            Assert.Equal(0, options.Port);
            Assert.Null(options.HostName);
            Assert.Null(options.QueueName);
            Assert.Null(options.UserName);
            Assert.Null(options.Password);
            Assert.Null(options.ConnectionString);

            // Test formatted
            Assert.Equal(GetFormattedOption(options), options.Format());
        }

        [Fact]
        public void TestConfiguredRabbitMQOptions()
        {
            ushort expectedPrefetchCount = 100;
            int expectedPort = 8080;
            string expectedHostName = "someHostName";
            string expectedQueueName = "someQueueName";
            string expectedUserName = "someUserName";
            string expectedPassword = "somePassword";
            string expectedConnectionString = "someConnectionString";
            RabbitMQOptions options = new RabbitMQOptions()
            {
                Port = expectedPort,
                HostName = expectedHostName,
                QueueName = expectedQueueName,
                UserName = expectedUserName,
                Password = expectedPassword,
                ConnectionString = expectedConnectionString,
                PrefetchCount = expectedPrefetchCount,
            };

            Assert.Equal(expectedPrefetchCount, options.PrefetchCount);
            Assert.Equal(expectedPort, options.Port);
            Assert.Equal(expectedHostName, options.HostName);
            Assert.Equal(expectedQueueName, options.QueueName);
            Assert.Equal(expectedUserName, options.UserName);
            Assert.Equal(expectedPassword, options.Password);
            Assert.Equal(expectedConnectionString, options.ConnectionString);

            // Test formatted
            Assert.Equal(GetFormattedOption(options), options.Format());
        }

        [Fact]
        public void TestJobHostHasTheRightConfiguration()
        {
            ushort expectedPrefetchCount = 10;

            var builder = new HostBuilder()
              .UseEnvironment("Development")
              .ConfigureWebJobs(webJobsBuilder =>
              {
                  webJobsBuilder
                  .AddRabbitMQ(a => a.PrefetchCount = expectedPrefetchCount); // set to non-default prefetch count
              })
              .UseConsoleLifetime();

            var host = builder.Build();
            using (host)
            {
                var config = host.Services.GetService<IOptions<RabbitMQOptions>>();
                Assert.Equal(config.Value.PrefetchCount, expectedPrefetchCount);
            }
        }
    }
}
