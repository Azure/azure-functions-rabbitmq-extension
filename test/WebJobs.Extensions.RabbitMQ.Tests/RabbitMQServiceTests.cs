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
        [InlineData("", "localhost", "queue", "guest", "guest", 5672, "", "localhost", "guest", "guest", 5672)]
        [InlineData("amqp://user:PASSWORD@52.175.195.81:5672", null, "queue", null, null, null, "amqp://user:PASSWORD@52.175.195.81:5672", "52.175.195.81", "user", "PASSWORD", 5672)]
        [InlineData("", "localhost", "queue", null, null, 0, "", "localhost", "guest", "guest", -1)] // Should fill in "guest", "guest", 5672
        public void Handles_Connection_Attributes_And_Options(string connectionString, string hostName, string queueName, string userName, string password, int port,
            string expectedConnectionString, string expectedHostName, string expectedUserName, string expectedPassword, int expectedPort)
        {
            RabbitMQService service = new RabbitMQService(connectionString, hostName, queueName, userName, password, port);

            ConnectionFactory factory = service.CreateConnectionFactory();

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

