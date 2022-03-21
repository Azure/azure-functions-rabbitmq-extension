// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class PocoToBytesConverter<T> : IConverter<T, ReadOnlyMemory<byte>>
    {
        public ReadOnlyMemory<byte> Convert(T input)
        {
            _ = input ?? throw new ArgumentNullException(nameof(input));

            string res = JsonConvert.SerializeObject(input);
            return Encoding.UTF8.GetBytes(res);
        }
    }
}
