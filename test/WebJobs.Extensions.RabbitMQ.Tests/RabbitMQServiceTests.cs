// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using RabbitMQ.Client;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQServiceTests
    {
        [Theory]
        [InlineData("", "localhost", "guest", "guest", 5672, "some-vhost", "", "localhost", "guest", "guest", 5672, "some-vhost")]
        [InlineData("amqp://testUserName:testPassword@11.111.111.11:5672/some-vhost", null, null, null, null, "some-vhost", "amqp://testUserName:testPassword@11.111.111.11:5672/some-vhost", "11.111.111.11", "testUserName", "testPassword", 5672, "some-vhost")]
        [InlineData("", "localhost", null, null, 0, "some-vhost", "", "localhost", "guest", "guest", -1, "some-vhost")] // Should fill in "guest", "guest", 5672
        public void Handles_Connection_Attributes_And_Options(string connectionString, string hostName, string userName, string password, int port, string virtualHost,
            string expectedConnectionString, string expectedHostName, string expectedUserName, string expectedPassword, int expectedPort, string expectedVirtualHost)
        {
            ConnectionFactory factory = RabbitMQService.GetConnectionFactory(connectionString, hostName, userName, password, port, virtualHost);

            if (String.IsNullOrEmpty(connectionString))
            {
                Assert.Null(factory.Uri);
                Assert.Equal(expectedHostName, factory.HostName);
                Assert.Equal(expectedUserName, factory.UserName);
                Assert.Equal(expectedPassword, factory.Password);
                Assert.Equal(expectedPort, factory.Port);
                Assert.Equal(expectedVirtualHost, factory.VirtualHost);
            }
            else
            {
                Assert.Equal(new Uri(expectedConnectionString), factory.Uri);
                Assert.Equal(expectedHostName, factory.HostName);
                Assert.Equal(expectedUserName, factory.UserName);
                Assert.Equal(expectedPassword, factory.Password);
                Assert.Equal(expectedPort, factory.Port);
                Assert.Equal(expectedVirtualHost, factory.VirtualHost);
            }
        }
    }
}

