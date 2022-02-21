﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
        private IConfiguration _emptyConfig = new ConfigurationBuilder().AddJsonFile("testappsettings.json").Build();

        [Theory]
        [InlineData("", "hello", "hello")]
        [InlineData("rabbitMQTest", "hello", "amqp://guest:guest@tada:5672")]
        public void ResolveConnectionString(string attributeConnectionString, string optionsConnectionString, string expectedResolvedString)
        {
            string resolvedString = Utility.ResolveConnectionString(attributeConnectionString, optionsConnectionString, _emptyConfig);
            Assert.Equal(expectedResolvedString, resolvedString);
        }
    }
}
