// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQTriggerBindingTests
    {
        [Fact]
        public void Verify_BindingDataContract_Types()
        {
            var expectedContract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            expectedContract.Add("ConsumerTag", typeof(string));
            expectedContract.Add("DeliveryTag", typeof(ulong));
            expectedContract.Add("Redelivered", typeof(bool));
            expectedContract.Add("Exchange", typeof(string));
            expectedContract.Add("RoutingKey", typeof(string));
            expectedContract.Add("BasicProperties", typeof(IBasicProperties));
            expectedContract.Add("Body", typeof(ReadOnlyMemory<byte>));

            var actualContract = RabbitMQTriggerBinding.CreateBindingDataContract();

            foreach (KeyValuePair<string, Type> item in actualContract)
            {
                Assert.Equal(expectedContract[item.Key], item.Value);
            }
        }

        [Fact]
        public void Verify_BindingDataContract_Values()
        {
            var data = new Dictionary<string, Object>(StringComparer.OrdinalIgnoreCase);
            data.Add("ConsumerTag", "ConsumerName");
            ulong deliveryTag = 1;
            data.Add("DeliveryTag", deliveryTag);
            data.Add("Redelivered", false);
            data.Add("RoutingKey", "QueueName");

            Random rand = new Random();
            byte[] buffer = new byte[10];
            rand.NextBytes(buffer);

            ReadOnlyMemory<byte> body = buffer;
            data.Add("Body", body);

            BasicDeliverEventArgs eventArgs = new BasicDeliverEventArgs("ConsumerName", deliveryTag, false, "n/a", "QueueName", null, body);
            data.Add("Exchange", eventArgs.Exchange);
            data.Add("BasicProperties", eventArgs.BasicProperties);

            var actualContract = RabbitMQTriggerBinding.CreateBindingData(eventArgs);

            foreach (KeyValuePair<string, Object> item in actualContract)
            {
                Assert.Equal(data[item.Key], item.Value);
            }
        }
    }
}
