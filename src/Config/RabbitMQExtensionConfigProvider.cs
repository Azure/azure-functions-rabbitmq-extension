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
        }

        public void ValidateBinding(RabbitMQAttribute attribute, Type type)
        {
            string hostname = Utility.FirstOrDefault(attribute.Hostname, this.options.Value.Hostname);
            string queuename = Utility.FirstOrDefault(attribute.QueueName, this.options.Value.QueueName);

            if (string.IsNullOrEmpty(hostname))
            {
                throw new InvalidOperationException("Missing hostname");
            }

            if (string.IsNullOrEmpty(queuename))
            {
                throw new InvalidOperationException("Missing queuename");
            }
        }

        internal RabbitMQContext CreateContext(RabbitMQAttribute attribute)
        {
            string hostname = Utility.FirstOrDefault(attribute.Hostname, string.Empty);
            string queuename = Utility.FirstOrDefault(attribute.QueueName, string.Empty);

            var context = new RabbitMQContext
            {
                Hostname = hostname,
                QueueName = queuename,
                Message = Utility.FirstOrDefault(attribute.Message, string.Empty),
            };

            return context;
        }
    }
}
