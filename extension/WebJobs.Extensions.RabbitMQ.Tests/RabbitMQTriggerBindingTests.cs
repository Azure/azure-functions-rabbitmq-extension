// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests;

public class RabbitMQTriggerBindingTests
{
    [Fact]
    public void Verify_BindingDataContract_Types()
    {
        var expectedContract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConsumerTag"] = typeof(string),
            ["DeliveryTag"] = typeof(ulong),
            ["Redelivered"] = typeof(bool),
            ["Exchange"] = typeof(string),
            ["RoutingKey"] = typeof(string),
            ["BasicProperties"] = typeof(IBasicProperties),
            ["Body"] = typeof(ReadOnlyMemory<byte>),
        };

        IReadOnlyDictionary<string, Type> actualContract = RabbitMQTriggerBinding.CreateBindingDataContract();

        foreach (KeyValuePair<string, Type> item in actualContract)
        {
            Assert.Equal(expectedContract[item.Key], item.Value);
        }
    }

    [Fact]
    public void Verify_BindingDataContract_Values()
    {
        ulong deliveryTag = 1;

        var rand = new Random();
        byte[] buffer = new byte[10];
        rand.NextBytes(buffer);

        ReadOnlyMemory<byte> body = buffer;
        var eventArgs = new BasicDeliverEventArgs("ConsumerName", deliveryTag, false, "n/a", "QueueName", null, body);

        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConsumerTag"] = "ConsumerName",
            ["DeliveryTag"] = deliveryTag,
            ["Redelivered"] = false,
            ["RoutingKey"] = "QueueName",
            ["Body"] = body,
            ["Exchange"] = eventArgs.Exchange,
            ["BasicProperties"] = eventArgs.BasicProperties,
        };

        IReadOnlyDictionary<string, object> actualContract = RabbitMQTriggerBinding.CreateBindingData(eventArgs);

        foreach (KeyValuePair<string, object> item in actualContract)
        {
            Assert.Equal(data[item.Key], item.Value);
        }
    }
}
