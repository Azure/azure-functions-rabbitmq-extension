// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Tests;

public class PocoToBytesConverterTests
{
    [Fact]
    public void Converts_String_Correctly()
    {
        var sampleObj = new TestClass { X = 1, Y = 1 };
        string res = JsonConvert.SerializeObject(sampleObj);
        byte[] expectedRes = Encoding.UTF8.GetBytes(res);

        var converter = new PocoToBytesConverter<TestClass>();
        byte[] actualRes = converter.Convert(sampleObj).ToArray();

        Assert.Equal(expectedRes, actualRes);
    }

    [Fact]
    public void NullString_Throws_Exception()
    {
        var converter = new PocoToBytesConverter<TestClass>();
        Assert.Throws<ArgumentNullException>(() => converter.Convert(null));
    }

    private sealed class TestClass
    {
        public int X { get; set; }

        public int Y { get; set; }
    }
}
