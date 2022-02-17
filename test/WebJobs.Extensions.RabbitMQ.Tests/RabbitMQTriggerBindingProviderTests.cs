// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Moq;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQTriggerBindingProviderTests
    {
        [Fact]
        public void Null_Context_Throws_Error()
        {
            var mockProvider = new Mock<RabbitMQTriggerAttributeBindingProvider>();
            Assert.ThrowsAsync<ArgumentNullException>(() => mockProvider.Object.TryCreateAsync(null));
        }
    }
}
