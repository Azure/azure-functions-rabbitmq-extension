// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class RabbitMQTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly INameResolver nameResolver;
        private readonly RabbitMQExtensionConfigProvider provider;
        private readonly ILogger logger;
        private readonly IOptions<RabbitMQOptions> options;
        private readonly IConfiguration configuration;

        public RabbitMQTriggerAttributeBindingProvider(
            INameResolver nameResolver,
            RabbitMQExtensionConfigProvider provider,
            ILogger logger,
            IOptions<RabbitMQOptions> options,
            IConfiguration configuration)
        {
            this.nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options;
            this.configuration = configuration;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));

            ParameterInfo parameter = context.Parameter;
            RabbitMQTriggerAttribute attribute = parameter.GetCustomAttribute<RabbitMQTriggerAttribute>(inherit: false);

            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            string connectionString = Utility.ResolveConnectionString(attribute.ConnectionStringSetting, options.Value.ConnectionString, configuration);
            string queueName = Resolve(attribute.QueueName) ?? throw new InvalidOperationException("RabbitMQ queue name is missing");
            bool disableCertificateValidation = attribute.DisableCertificateValidation || options.Value.DisableCertificateValidation;

            IRabbitMQService service = provider.GetService(connectionString, queueName, disableCertificateValidation);

            return Task.FromResult<ITriggerBinding>(new RabbitMQTriggerBinding(service, queueName, logger, parameter.ParameterType, options.Value.PrefetchCount));
        }

        private string Resolve(string name)
        {
            return nameResolver.ResolveWholeString(name) ?? name;
        }
    }
}
