// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    [Extension("RabbitMQ")]
    internal class RabbitMQExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IOptions<RabbitMQOptions> _options;
        private readonly INameResolver _nameResolver;
        private readonly IRabbitMQServiceFactory _rabbitMQServiceFactory;
        private ILogger _logger;

        public RabbitMQExtensionConfigProvider(IOptions<RabbitMQOptions> options, INameResolver nameResolver, IRabbitMQServiceFactory rabbitMQServiceFactory, ILoggerFactory loggerFactory)
        {
            _options = options;
            _nameResolver = nameResolver;
            _rabbitMQServiceFactory = rabbitMQServiceFactory;
            _logger = loggerFactory?.CreateLogger(LogCategories.CreateTriggerCategory("RabbitMQ"));
        }

        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var rule = context.AddBindingRule<RabbitMQAttribute>();
            rule.AddValidator(this.ValidateBinding);
            rule.BindToCollector<byte[]>((attr) =>
            {
                return new RabbitMQAsyncCollector(this.CreateContext(attr), _logger);
            });
            rule.AddConverter<string, byte[]>(msg => Encoding.UTF8.GetBytes(msg));
            rule.AddOpenConverter<OpenType.Poco, byte[]>(typeof(PocoToBytesConverter<>));

            var triggerRule = context.AddBindingRule<RabbitMQTriggerAttribute>();
            triggerRule.BindToTrigger<BasicDeliverEventArgs>(new RabbitMQTriggerAttributeBindingProvider(
                    _nameResolver,
                    _options,
                    this,
                    _logger));

            // Converts BasicDeliverEventArgs to string so user can extract received message.
            triggerRule.AddConverter<BasicDeliverEventArgs, string>(args => Encoding.UTF8.GetString(args.Body))
                .AddConverter<BasicDeliverEventArgs, DirectInvokeString>((args) => new DirectInvokeString(null));

            // Convert BasicDeliverEventArgs --> string-- > JSON-- > POCO
            triggerRule.AddOpenConverter<BasicDeliverEventArgs, OpenType.Poco>(typeof(BasicDeliverEventArgsToPocoConverter<>), _logger);
        }

        public void ValidateBinding(RabbitMQAttribute attribute, Type type)
        {
            string hostname = Utility.FirstOrDefault(attribute.HostName, _options.Value.Hostname);
            string queuename = Utility.FirstOrDefault(attribute.QueueName, _options.Value.QueueName);

            if (string.IsNullOrEmpty(hostname))
            {
                throw new InvalidOperationException("RabbitMQ hostname is missing");
            }

            if (string.IsNullOrEmpty(queuename))
            {
                throw new InvalidOperationException("RabbitMQ queuename is missing");
            }
        }

        internal RabbitMQContext CreateContext(RabbitMQAttribute attribute)
        {
            string hostname = Utility.FirstOrDefault(attribute.HostName, _options.Value.Hostname);
            string queuename = Utility.FirstOrDefault(attribute.QueueName, _options.Value.QueueName);
            string exchange = Utility.FirstOrDefault(attribute.Exchange, _options.Value.Exchange) ?? string.Empty;
            IBasicProperties properties = attribute.Properties;

            var resolvedAttribute = new RabbitMQAttribute
            {
                HostName = hostname,
                QueueName = queuename,
                Exchange = exchange,
                Properties = properties,
            };

            IRabbitMQService service = GetService(hostname, queuename);

            return new RabbitMQContext
            {
                ResolvedAttribute = resolvedAttribute,
                Service = service,
            };
        }

        internal IRabbitMQService GetService(string hostname, string queuename)
        {
            return _rabbitMQServiceFactory.CreateService(hostname, queuename);
        }
    }
}
