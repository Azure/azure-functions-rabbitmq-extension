using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class UtilityTests
    {
        [Fact]
        public void InvalidCredentials_ReturnsFalse()
        {
            string host = "52.175.195.81";
            string userName = "";
            string password = "";

            Assert.False(Utility.ValidateUserNamePassword(userName, password, host));
        }

        [Fact]
        public void ValidCredentials_ReturnsTrue()
        {
            string host = "52.175.195.81";
            string userName = "user";
            string password = "PASSWORD";

            Assert.True(Utility.ValidateUserNamePassword(userName, password, host));
        }
    }
}
