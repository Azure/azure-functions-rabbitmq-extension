// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    [Extension("RabbitMQ")]
    internal class RabbitMQExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IOptions<RabbitMQOptions> _options;
        private readonly IRabbitMQServiceFactory _rabbitMQServiceFactory;
        private readonly ILoggerFactory _loggerFactory;

        public RabbitMQExtensionConfigProvider(IOptions<RabbitMQOptions> options, IRabbitMQServiceFactory rabbitMQServiceFactory, ILoggerFactory loggerFactory)
        {
            _options = options;
            _rabbitMQServiceFactory = rabbitMQServiceFactory;
            _loggerFactory = loggerFactory;
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
                return new RabbitMQAsyncCollector(this.CreateContext(attr), _loggerFactory.CreateLogger<RabbitMQAsyncCollector>());
            });
            rule.AddConverter<string, byte[]>(msg => Encoding.UTF8.GetBytes(msg));
            rule.AddOpenConverter<OpenType.Poco, byte[]>(typeof(PocoToBytesConverter<>));
        }

        public void ValidateBinding(RabbitMQAttribute attribute, Type type)
        {
            string hostname = Utility.FirstOrDefault(attribute.Hostname, _options.Value.Hostname);
            string queuename = Utility.FirstOrDefault(attribute.QueueName, _options.Value.QueueName);
            string exchange = Utility.FirstOrDefault(attribute.Exchange, _options.Value.Exchange);
            IBasicProperties properties = attribute.Properties;

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
            string hostname = Utility.FirstOrDefault(attribute.Hostname, _options.Value.Hostname);
            string queuename = Utility.FirstOrDefault(attribute.QueueName, _options.Value.QueueName);
            string exchange = Utility.FirstOrDefault(attribute.Exchange, _options.Value.Exchange) ?? string.Empty;
            IBasicProperties properties = attribute.Properties;

            var resolvedAttribute = new RabbitMQAttribute
            {
                Hostname = hostname,
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

        internal class PocoToBytesConverter<T> : IConverter<T, byte[]>
        {
            public byte[] Convert(T input)
            {
                string res = JsonConvert.SerializeObject(input);
                return Encoding.UTF8.GetBytes(res);
            }
        }
    }
}
