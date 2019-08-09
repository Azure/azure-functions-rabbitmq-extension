// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class RabbitMQTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly INameResolver _nameResolver;
        private readonly RabbitMQExtensionConfigProvider _provider;

        public RabbitMQTriggerAttributeBindingProvider(
            INameResolver nameResolver,
            RabbitMQExtensionConfigProvider provider)
        {
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            RabbitMQTriggerAttribute attribute = parameter.GetCustomAttribute<RabbitMQTriggerAttribute>(inherit: false);

            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            string queueName = Resolve(attribute.QueueName);

            string hostName = Resolve(attribute.HostName);

            string userName = Resolve(attribute.UserName);

            string password = Resolve(attribute.Password);

            int port = attribute.Port;

            ushort batchNumber = attribute.BatchNumber;

            if (string.IsNullOrEmpty(hostName))
            {
                throw new InvalidOperationException("RabbitMQ host name is missing");
            }

            if (string.IsNullOrEmpty(queueName))
            {
                throw new InvalidOperationException("RabbitMQ queue name is missing");
            }

            if ((string.IsNullOrEmpty(userName) && hostName != "localhost") ||
                (string.IsNullOrEmpty(password) && hostName != "localhost"))
            {
                throw new InvalidOperationException("RabbitMQ username and password required if not connecting to localhost");
            }

            // If there's no specified batch number, default to 1
            if (batchNumber == 0)
            {
                batchNumber = 1;
            }

            IRabbitMQService service = _provider.GetService(hostName, queueName, userName, password, port);

            return Task.FromResult<ITriggerBinding>(new RabbitMQTriggerBinding(service, hostName, queueName, batchNumber));
        }

        private string Resolve(string name)
        {
            return _nameResolver.ResolveWholeString(name) ?? name;
        }
    }
}
