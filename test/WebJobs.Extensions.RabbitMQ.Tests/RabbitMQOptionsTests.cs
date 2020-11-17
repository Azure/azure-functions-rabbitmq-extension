// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQOptionsTests
    {
        private string GetFormattedOption(RabbitMQOptions option)
        {
            JObject prefetchOptions = null;
            if (option.PrefetchOptions != null)
            {
                prefetchOptions = new JObject
                {
                    { nameof(PrefetchOptions.PrefetchSize), option.PrefetchOptions.PrefetchSize },
                    { nameof(PrefetchOptions.PrefetchCount), option.PrefetchOptions.PrefetchCount },
                };
            }

            JObject options = new JObject
            {
                { nameof(option.HostName), option.HostName },
                { nameof(option.QueueName), option.QueueName },
                { nameof(option.Port), option.Port },
                { nameof(PrefetchOptions), prefetchOptions },
            };

            return options.ToString(Formatting.Indented);
        }

        [Fact]
        public void TestDefaultOptions()
        {
            RabbitMQOptions options = new RabbitMQOptions();

            Assert.Equal<ushort>(30, options.PrefetchOptions.PrefetchCount);
            Assert.Equal<uint>(0, options.PrefetchOptions.PrefetchSize);
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
        public void TestPartialDefaultPrefetchOptions_PrefetchCount()
        {
            uint expectedPrefetchSize = 0;
            ushort expectedPrefetchCount = 100;
            RabbitMQOptions options = new RabbitMQOptions()
            {
                PrefetchOptions = new PrefetchOptions()
                {
                    PrefetchCount = expectedPrefetchCount,
                }
            };

            Assert.Equal(expectedPrefetchCount, options.PrefetchOptions.PrefetchCount);
            Assert.Equal(expectedPrefetchSize, options.PrefetchOptions.PrefetchSize);

            // Test formatted
            Assert.Equal(GetFormattedOption(options), options.Format());
        }

        [Fact]
        public void TestPartialDefaultPrefetchOptions_PrefetchSize()
        {
            uint expectedPrefetchSize = 10;
            ushort expectedPrefetchCount = 30;
            RabbitMQOptions options = new RabbitMQOptions()
            {
                PrefetchOptions = new PrefetchOptions()
                {
                    PrefetchSize = expectedPrefetchSize,
                }
            };

            Assert.Equal(expectedPrefetchCount, options.PrefetchOptions.PrefetchCount);
            Assert.Equal(expectedPrefetchSize, options.PrefetchOptions.PrefetchSize);

            // Test formatted
            Assert.Equal(GetFormattedOption(options), options.Format());
        }

        [Fact]
        public void TestConfiguredRabbitMQOptions()
        {
            uint expectedPrefetchSize = 10;
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
                PrefetchOptions = new PrefetchOptions()
                {
                    PrefetchSize = expectedPrefetchSize,
                    PrefetchCount = expectedPrefetchCount,
                }
            };

            Assert.Equal(expectedPrefetchCount, options.PrefetchOptions.PrefetchCount);
            Assert.Equal(expectedPrefetchSize, options.PrefetchOptions.PrefetchSize);
            Assert.Equal(expectedPort, options.Port);
            Assert.Equal(expectedHostName, options.HostName);
            Assert.Equal(expectedQueueName, options.QueueName);
            Assert.Equal(expectedUserName, options.UserName);
            Assert.Equal(expectedPassword, options.Password);
            Assert.Equal(expectedConnectionString, options.ConnectionString);

            // Test formatted
            Assert.Equal(GetFormattedOption(options), options.Format());
        }
    }
}
