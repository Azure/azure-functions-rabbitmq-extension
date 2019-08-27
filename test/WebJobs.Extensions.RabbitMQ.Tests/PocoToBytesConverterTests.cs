// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Newtonsoft.Json;
using Xunit;

namespace WebJobs.Extensions.RabbitMQ.Tests
{
    public class PocoToBytesConverterTests
    {
        [Fact]
        public void Converts_String_Correctly()
        {
            TestClass sampleObj = new TestClass(1, 1);
            string res = JsonConvert.SerializeObject(sampleObj);
            byte[] expectedRes = Encoding.UTF8.GetBytes(res);

            PocoToBytesConverter<TestClass> converter = new PocoToBytesConverter<TestClass>();
            byte[] actualRes = converter.Convert(sampleObj);

            Assert.Equal(expectedRes, actualRes);
        }

        [Fact]
        public void NullString_Throws_Exception()
        {
            PocoToBytesConverter<TestClass> converter = new PocoToBytesConverter<TestClass>();
            Assert.Throws<ArgumentNullException>(() => converter.Convert(null));
        }

        public class TestClass
        {
            public int x, y;

            public TestClass(int x, int y)
            {
                x = x;
                y = y;
            }
        }
    }
}
