using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class UtilityTests
    {
        [Theory]
        [InlineData("52.175.195.81", "", "")]
        [InlineData("52.175.195.81", "user", "PASSWORD")]
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
    }
}
