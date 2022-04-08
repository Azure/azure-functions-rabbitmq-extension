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
        private readonly BasicDeliverEventArgs _input;

        public BasicDeliverEventArgsValueProvider(BasicDeliverEventArgs input, Type destinationType)
        {
            _input = input;
            Type = destinationType;
        }

        public Type Type { get; }

        public Task<object> GetValueAsync()
        {
            if (Type.Equals(typeof(BasicDeliverEventArgs)))
            {
                return Task.FromResult<object>(_input);
            }
            else if (Type.Equals(typeof(ReadOnlyMemory<byte>)))
            {
                return Task.FromResult<object>(_input.Body);
            }
            else if (Type.Equals(typeof(byte[])))
            {
                return Task.FromResult<object>(_input.Body.ToArray());
            }

            string inputValue = ToInvokeString();
            if (Type.Equals(typeof(string)))
            {
                return Task.FromResult<object>(inputValue);
            }
            else
            {
                try
                {
                    return Task.FromResult(JsonConvert.DeserializeObject(inputValue, Type));
                }
                catch (JsonException e)
                {
                    // Give useful error if object in queue is not deserialized properly.
                    string msg = $@"Binding parameters to complex objects (such as '{Type.Name}') uses Json.NET serialization. The JSON parser failed: {e.Message}";
                    throw new InvalidOperationException(msg, e);
                }
            }
        }

        public string ToInvokeString()
        {
            return Encoding.UTF8.GetString(_input.Body.ToArray());
        }
    }
}
