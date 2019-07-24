// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    [Extension("RabbitMQ")]
    internal class RabbitMQExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IOptions<RabbitMQOptions> options;

        public RabbitMQExtensionConfigProvider(IOptions<RabbitMQOptions> options)
        {
            this.options = options;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var rule = context.AddBindingRule<RabbitMQAttribute>();
            rule.AddValidator(this.ValidateBinding);
            rule.BindToCollector<string>((attr) =>
            {
                return new RabbitMQAsyncCollector(this.CreateContext(attr));
            });
            rule.AddOpenConverter<OpenType.Poco, string>(typeof(PocoToStringConverter<>));
        }

        public void ValidateBinding(RabbitMQAttribute attribute, Type type)
        {
            string hostname = Utility.FirstOrDefault(attribute.Hostname, this.options.Value.Hostname);
            string queuename = Utility.FirstOrDefault(attribute.QueueName, this.options.Value.QueueName);
            string exchange = Utility.FirstOrDefault(attribute.Exchange, this.options.Value.Exchange);
            IBasicProperties properties = attribute.Properties;

            if (string.IsNullOrEmpty(hostname))
            {
                throw new InvalidOperationException("RabbitMQ hostname is missing");
            }

            if (string.IsNullOrEmpty(queuename))
            {
                throw new InvalidOperationException("RabbitMQ queuename is missing");
            }

            if (exchange == null)
            {
                exchange = string.Empty;
            }
        }

        internal RabbitMQContext CreateContext(RabbitMQAttribute attribute)
        {
            string hostname = Utility.FirstOrDefault(attribute.Hostname, this.options.Value.Hostname);
            string queuename = Utility.FirstOrDefault(attribute.QueueName, this.options.Value.QueueName);
            string exchange = Utility.FirstOrDefault(attribute.Exchange, this.options.Value.Exchange);
            IBasicProperties properties = attribute.Properties;

            if (exchange == null)
            {
                exchange = string.Empty;
            }

            var context = new RabbitMQContext
            {
                Hostname = hostname,
                QueueName = queuename,
                Exchange = exchange,
                Properties = properties
            };

            return context;
        }

        private class PocoToStringConverter<T> : IConverter<T, string>
        {
            public string Convert(T input)
            {
                return JsonConvert.SerializeObject(input);
            }
        }
    }
}
