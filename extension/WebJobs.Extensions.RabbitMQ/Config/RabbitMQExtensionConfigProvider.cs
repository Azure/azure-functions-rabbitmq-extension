// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    [Extension("RabbitMQ")]
    internal class RabbitMQExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IOptions<RabbitMQOptions> _options;
        private readonly INameResolver _nameResolver;
        private readonly IRabbitMQServiceFactory _rabbitMQServiceFactory;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, IRabbitMQService> _connectionParametersToService;

        public RabbitMQExtensionConfigProvider(IOptions<RabbitMQOptions> options, INameResolver nameResolver, IRabbitMQServiceFactory rabbitMQServiceFactory, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _options = options;
            _nameResolver = nameResolver;
            _rabbitMQServiceFactory = rabbitMQServiceFactory;
            _logger = loggerFactory?.CreateLogger(LogCategories.CreateTriggerCategory("RabbitMQ"));
            _configuration = configuration;
            _connectionParametersToService = new ConcurrentDictionary<string, IRabbitMQService>();
        }

        public void Initialize(ExtensionConfigContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));

#pragma warning disable 0618
            FluentBindingRule<RabbitMQAttribute> rule = context.AddBindingRule<RabbitMQAttribute>();
#pragma warning restore 0618

            rule.BindToCollector<ReadOnlyMemory<byte>>((attr) =>
            {
                return new RabbitMQAsyncCollector(CreateContext(attr), _logger);
            });
            rule.BindToInput<IModel>(new RabbitMQClientBuilder(this, _options));
            rule.AddConverter<string, ReadOnlyMemory<byte>>(arg => Encoding.UTF8.GetBytes(arg));
            rule.AddConverter<byte[], ReadOnlyMemory<byte>>(arg => arg);
            rule.AddOpenConverter<OpenType.Poco, ReadOnlyMemory<byte>>(typeof(PocoToBytesConverter<>));

#pragma warning disable 0618
            FluentBindingRule<RabbitMQTriggerAttribute> triggerRule = context.AddBindingRule<RabbitMQTriggerAttribute>();
#pragma warning restore 0618

            // More details about why the BindToTrigger was chosen instead of BindToTrigger<BasicDeliverEventArgs> detailed here https://github.com/Azure/azure-functions-rabbitmq-extension/issues/110
            triggerRule.BindToTrigger(new RabbitMQTriggerAttributeBindingProvider(
                    _nameResolver,
                    this,
                    _logger,
                    _options,
                    _configuration));
        }

        internal RabbitMQContext CreateContext(RabbitMQAttribute attribute)
        {
            string connectionString = Utility.FirstOrDefault(attribute.ConnectionStringSetting, _options.Value.ConnectionString);
            string queueName = Utility.FirstOrDefault(attribute.QueueName, _options.Value.QueueName);
            bool disableCertificateValidation = Utility.FirstOrDefault(attribute.DisableCertificateValidation, _options.Value.DisableCertificateValidation);

            var resolvedAttribute = new RabbitMQAttribute
            {
                ConnectionStringSetting = connectionString,
                QueueName = queueName,
                DisableCertificateValidation = disableCertificateValidation,
            };

            IRabbitMQService service = GetService(connectionString, queueName, disableCertificateValidation);

            return new RabbitMQContext
            {
                ResolvedAttribute = resolvedAttribute,
                Service = service,
            };
        }

        internal IRabbitMQService GetService(string connectionString, string queueName, bool disableCertificateValidation)
        {
            string[] keyArray = { connectionString, queueName, disableCertificateValidation.ToString() };
            string key = string.Join(",", keyArray);
            return _connectionParametersToService.GetOrAdd(key, _ => _rabbitMQServiceFactory.CreateService(connectionString, queueName, disableCertificateValidation));
        }

        // Overloaded method used only for getting the RabbitMQ client
        internal IRabbitMQService GetService(string connectionString, bool disableCertificateValidation)
        {
            string[] keyArray = { connectionString, disableCertificateValidation.ToString() };
            string key = string.Join(",", keyArray);
            return _connectionParametersToService.GetOrAdd(key, _ => _rabbitMQServiceFactory.CreateService(connectionString, disableCertificateValidation));
        }
    }
}
