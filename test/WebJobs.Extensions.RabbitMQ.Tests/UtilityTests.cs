using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class UtilityTests
    {
        private IConfiguration _emptyConfig = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        [Theory]
        [InlineData("11.111.111.11", "", "")]
        [InlineData("11.111.111.11", "testUserName", "testPassword")]
        public void ValidateCredentials(string hostName, string userName, string password)
        {
            if (String.IsNullOrEmpty(userName))
            {
                Assert.False(Utility.ValidateUserNamePassword(userName, password, hostName));
            } 
            else
            {
                Assert.True(Utility.ValidateUserNamePassword(userName, password, hostName));
            }
        }

        [Theory]
        [InlineData("", "hello", "hello")]
        [InlineData("rabbitMQTest", "hello", "amqp://guest:guest@tada:5672")]
        public void ResolveConnectionString(string attributeConnectionString, string optionsConnectionString, string e)
        {
            string resolvedString = Utility.ResolveConnectionString(attributeConnectionString, optionsConnectionString, _emptyConfig);

            if (string.IsNullOrEmpty(attributeConnectionString))
            {
                Assert.Equal("hello", resolvedString);
            }
            else
            {
                Assert.Equal("amqp://guest:guest@tada:5672", resolvedString);
            }
        }
    }
}
