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
    public class EventArgsValueProvider : IValueProvider
    {
        private readonly BasicDeliverEventArgs _input;

        public EventArgsValueProvider(BasicDeliverEventArgs input, Type destinationType)
        {
            _input = input;
            Type = destinationType;
        }

        public Type Type { get; }

        public Task<object> GetValueAsync()
        {
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
                    string msg = string.Format(@"Binding parameters to complex objects (such as '{0}') uses Json.NET serialization. The JSON parser failed: {1}", Type.Name, e.Message);
                    throw new InvalidOperationException(msg);
                }
            }
        }

        public string ToInvokeString()
        {
            return Encoding.UTF8.GetString(_input.Body);
        }
    }
}
