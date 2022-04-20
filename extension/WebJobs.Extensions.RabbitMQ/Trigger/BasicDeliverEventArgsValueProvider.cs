// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class BasicDeliverEventArgsValueProvider : IValueProvider
    {
        private readonly BasicDeliverEventArgs input;

        public BasicDeliverEventArgsValueProvider(BasicDeliverEventArgs input, Type destinationType)
        {
            this.input = input;
            this.Type = destinationType;
        }

        public Type Type { get; }

        public Task<object> GetValueAsync()
        {
            if (this.Type.Equals(typeof(BasicDeliverEventArgs)))
            {
                return Task.FromResult<object>(this.input);
            }
            else if (this.Type.Equals(typeof(ReadOnlyMemory<byte>)))
            {
                return Task.FromResult<object>(this.input.Body);
            }
            else if (this.Type.Equals(typeof(byte[])))
            {
                return Task.FromResult<object>(this.input.Body.ToArray());
            }

            string inputValue = this.ToInvokeString();
            if (this.Type.Equals(typeof(string)))
            {
                return Task.FromResult<object>(inputValue);
            }
            else
            {
                try
                {
                    return Task.FromResult(JsonConvert.DeserializeObject(inputValue, this.Type));
                }
                catch (JsonException e)
                {
                    // Give useful error if object in queue is not deserialized properly.
                    string msg = $@"Binding parameters to complex objects (such as '{this.Type.Name}') uses Json.NET serialization. The JSON parser failed: {e.Message}";
                    throw new InvalidOperationException(msg, e);
                }
            }
        }

        public string ToInvokeString()
        {
            return Encoding.UTF8.GetString(this.input.Body.ToArray());
        }
    }
}
