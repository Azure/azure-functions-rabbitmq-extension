// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using Xunit;


namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class BasicDeliverEventArgsToPocoConverterTests
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

            Assert.Equal(expectedObj.X, actualObj.X);
            Assert.Equal(expectedObj.Y, actualObj.Y);
        }

        [Fact]
        public void InvalidFormat_Throws_JsonException()
        {
            string str = "wrong format";
            byte[] strBytes = Encoding.UTF8.GetBytes(str);
            BasicDeliverEventArgs args = new BasicDeliverEventArgs("tag", 1, false, "", "queue", null, strBytes);

            ILoggerFactory loggerFactory = new LoggerFactory();
            ILogger logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("RabbitMQ"));
            BasicDeliverEventArgsToPocoConverter<TestClass> converter = new BasicDeliverEventArgsToPocoConverter<TestClass>(logger);
            Assert.Throws<JsonReaderException>(() => converter.Convert(args));
        }

        public class TestClass
        {
            private readonly int _x;
            private readonly int _y;

            public TestClass(int x, int y)
            {
                _x = x;
                _y = y;
            }

            public int X => _x;

            public int Y => _y;
        }
    }
}
