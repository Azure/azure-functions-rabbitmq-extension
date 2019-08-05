﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Trigger
{
    internal class RabbitMQTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly INameResolver _nameResolver;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly IOptions<RabbitMQOptions> _options;
        private readonly RabbitMQExtensionConfigProvider _provider;

        public RabbitMQTriggerAttributeBindingProvider(
            IConfiguration configuration,
            INameResolver nameResolver,
            IOptions<RabbitMQOptions> options,
            RabbitMQExtensionConfigProvider provider,
            ILoggerFactory loggerFactory)
        {
            _config = configuration;
            _nameResolver = nameResolver;
            _options = options;
            _provider = provider;
            _logger = loggerFactory?.CreateLogger(LogCategories.CreateTriggerCategory("RabbitMQ"));
        }

        public async Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            RabbitMQTriggerAttribute attribute = parameter.GetCustomAttribute<RabbitMQTriggerAttribute>(inherit: false);

            if (attribute == null)
            {
                return null;
            }

            string queueName = null;

            if (attribute.QueueName != null)
            {
                queueName = this.Resolve(attribute.QueueName);
            }

            string hostname = null;

            if (attribute.Hostname != null)
            {
                hostname = this.Resolve(attribute.Hostname);
            }

            ushort batchNumber = attribute.BatchNumber;

            IRabbitMQService service = _provider.GetService(hostname, queueName);

            return new RabbitMQTriggerBinding(service, hostname, queueName, batchNumber);
        }

        private string Resolve(string name)
        {
            if (_nameResolver == null)
            {
                return name;
            }

            return _nameResolver.ResolveWholeString(name);
        }
    }
}