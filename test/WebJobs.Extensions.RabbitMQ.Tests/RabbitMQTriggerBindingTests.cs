// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class RabbitMQTriggerBindingTests
    {
        [Fact]
        public void Converts_to_Poco()
        {
            TestClass expectedObj = new TestClass(1, 1);

            string objJson = JsonConvert.SerializeObject(expectedObj);
            byte[] objJsonBytes = Encoding.UTF8.GetBytes(objJson);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", null, objJsonBytes);

            ILoggerFactory loggerFactory = new LoggerFactory();
            ILogger logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("RabbitMQ"));
            BasicDeliverEventArgsToPocoConverter<TestClass> converter = new BasicDeliverEventArgsToPocoConverter<TestClass>(logger);
            TestClass actualObj = converter.Convert(args);

            Assert.Equal(expectedObj.x, actualObj.x);
            Assert.Equal(expectedObj.y, actualObj.y);
        }

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
            expectedContract.Add("Body", typeof(byte[]));

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
            byte[] body = new byte[10];
            rand.NextBytes(body);

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

        public class TestClass
        {
            public int x, y;

            public TestClass(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
    }
}
