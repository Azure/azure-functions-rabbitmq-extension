// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQServiceTests
    {
        [Theory]
        [InlineData("", "localhost", "guest", "guest", 5672, "", "localhost", "guest", "guest", 5672)]
        [InlineData("amqp://testUserName:testPassword@11.111.111.11:5672", null, null, null, 0, "amqp://testUserName:testPassword@11.111.111.11:5672", "11.111.111.11", "testUserName", "testPassword", 5672)]
        [InlineData("", "localhost", null, null, 0, "", "localhost", "guest", "guest", -1)] // Should fill in "guest", "guest", 5672
        public void Handles_Connection_Attributes_And_Options(string connectionString, string hostName, string userName, string password, int port,
            string expectedConnectionString, string expectedHostName, string expectedUserName, string expectedPassword, int expectedPort)
        {
            ConnectionFactory factory = RabbitMQService.GetConnectionFactory(connectionString, hostName, userName, password, port);

            if (String.IsNullOrEmpty(connectionString))
            {
                Assert.Null(factory.Uri);
                Assert.Equal(expectedHostName, factory.HostName);
                Assert.Equal(expectedUserName, factory.UserName);
                Assert.Equal(expectedPassword, factory.Password);
                Assert.Equal(expectedPort, factory.Port);
            }
            else
            {
                Assert.Equal(new Uri(expectedConnectionString), factory.Uri);
                Assert.Equal(expectedHostName, factory.HostName);
                Assert.Equal(expectedUserName, factory.UserName);
                Assert.Equal(expectedPassword, factory.Password);
                Assert.Equal(expectedPort, factory.Port);
            }
        }
    }
}

