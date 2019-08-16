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
        [InlineData(null, "localhost", "queue", "guest", "guest", 5672)]
        [InlineData("amqp://user:PASSWORD@52.175.195.81:5672", null, "queue", null, null, null)]
        [InlineData(null, "localhost", "queue", null, null, 0)] // Should fill in "guest", "guest", 5672
        public void Handles_Connection_Attributes_And_Options(string connectionString, string hostName, string queueName, string userName, string password, int port)
        {
            RabbitMQService service = new RabbitMQService(connectionString, hostName, queueName, userName, password, port);

            ConnectionFactory factory = service.CreateConnectionFactory();
            if (connectionString == null && userName == "guest")
            {
                Assert.Null(factory.Uri);
                Assert.Equal("localhost", factory.HostName);
                Assert.Equal("guest", factory.UserName);
                Assert.Equal("guest", factory.Password);
                Assert.Equal(5672, factory.Port);
            }
            else if (connectionString != null)
            {
                Assert.Equal(new Uri("amqp://user:PASSWORD@52.175.195.81:5672"), factory.Uri);
                Assert.Equal("52.175.195.81", factory.HostName);
                Assert.Equal("user", factory.UserName);
                Assert.Equal("PASSWORD", factory.Password);
                Assert.Equal(5672, factory.Port);
            }
            else if (port == 0)
            {
                Assert.Null(factory.Uri);
                Assert.Equal("localhost", factory.HostName);
                Assert.Equal("guest", factory.UserName);
                Assert.Equal("guest", factory.Password);
                Assert.Equal(-1, factory.Port);
            }
        }
    }
}

