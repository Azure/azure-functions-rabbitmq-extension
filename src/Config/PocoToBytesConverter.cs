// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class PocoToBytesConverter<T> : IConverter<T, byte[]>
    {
        public byte[] Convert(T input)
        {
            string res = JsonConvert.SerializeObject(input);
            return Encoding.UTF8.GetBytes(res);
        }
    }
}
