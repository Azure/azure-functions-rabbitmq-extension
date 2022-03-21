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
        private readonly INameResolver _nameResolver;
        private readonly RabbitMQExtensionConfigProvider _provider;
        private readonly ILogger _logger;
        private readonly IOptions<RabbitMQOptions> _options;
        private readonly IConfiguration _configuration;

        public RabbitMQTriggerAttributeBindingProvider(
            INameResolver nameResolver,
            RabbitMQExtensionConfigProvider provider,
            ILogger logger,
            IOptions<RabbitMQOptions> options,
            IConfiguration configuration)
        {
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options;
            _configuration = configuration;
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

            string connectionString = Utility.ResolveConnectionString(attribute.ConnectionStringSetting, _options.Value.ConnectionString, _configuration);
            string queueName = Resolve(attribute.QueueName) ?? throw new InvalidOperationException("RabbitMQ queue name is missing");
            bool disableCertificateValidation = attribute.DisableCertificateValidation || _options.Value.DisableCertificateValidation;

            IRabbitMQService service = _provider.GetService(connectionString, queueName, disableCertificateValidation);

            return Task.FromResult<ITriggerBinding>(new RabbitMQTriggerBinding(service, queueName, _logger, parameter.ParameterType, _options.Value.PrefetchCount));
        }

        private string Resolve(string name)
        {
            return _nameResolver.ResolveWholeString(name) ?? name;
        }
    }
}
