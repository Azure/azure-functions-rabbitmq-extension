// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using RabbitMQ.Client;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQServiceTests
    {
        [Theory]
        [InlineData("", "localhost", "guest", "guest", 5672, SslPolicyErrors.RemoteCertificateNameMismatch, "", "localhost", "guest", "guest", 5672, SslPolicyErrors.RemoteCertificateNameMismatch)]
        [InlineData("amqp://testUserName:testPassword@11.111.111.11:5672", null, null, null, null, SslPolicyErrors.RemoteCertificateNameMismatch, "amqp://testUserName:testPassword@11.111.111.11:5672", "11.111.111.11", "testUserName", "testPassword", 5672, SslPolicyErrors.RemoteCertificateNameMismatch)]
        [InlineData("amqps://testUserName:testPassword@11.111.111.11:5672", null, null, null, null, SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch, "amqps://testUserName:testPassword@11.111.111.11:5672", "11.111.111.11", "testUserName", "testPassword", 5672, SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch)]
        [InlineData("", "localhost", null, null, 0, SslPolicyErrors.RemoteCertificateNameMismatch, "", "localhost", "guest", "guest", -1, SslPolicyErrors.RemoteCertificateNameMismatch)] // Should fill in "guest", "guest", 5672
        public void Handles_Connection_Attributes_And_Options(string connectionString, string hostName, string userName, string password, int port, SslPolicyErrors acceptablePolicyErrors,
            string expectedConnectionString, string expectedHostName, string expectedUserName, string expectedPassword, int expectedPort, SslPolicyErrors expectedAcceptablePolicyErrors)
        {
            ConnectionFactory factory = RabbitMQService.GetConnectionFactory(connectionString, hostName, userName, password, port, acceptablePolicyErrors);

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

                if (connectionString.StartsWith("amqps://"))
                {
                    Assert.Equal(expectedAcceptablePolicyErrors, factory.Ssl.AcceptablePolicyErrors);
                }
            }
        }
    }
}

