// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class RabbitMQClientBuilder : IConverter<RabbitMQAttribute, IModel>
    {
        private readonly RabbitMQExtensionConfigProvider _configProvider;
        private readonly IOptions<RabbitMQOptions> _options;

        public RabbitMQClientBuilder(RabbitMQExtensionConfigProvider configProvider, IOptions<RabbitMQOptions> options)
        {
            _configProvider = configProvider;
            _options = options;
        }

        public IModel Convert(RabbitMQAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            string resolvedConnectionString = Utility.FirstOrDefault(attribute.ConnectionStringSetting, _options.Value.ConnectionString);
            string resolvedHostName = Utility.FirstOrDefault(attribute.HostName, _options.Value.HostName);
            string resolvedUserName = Utility.FirstOrDefault(attribute.UserName, _options.Value.UserName);
            string resolvedPassword = Utility.FirstOrDefault(attribute.Password, _options.Value.Password);
            int resolvedPort = Utility.FirstOrDefault(attribute.Port, _options.Value.Port);
            string resolvedVirtualHost = Utility.FirstOrDefault(attribute.VirtualHost, _options.Value.VirtualHost);

            IRabbitMQService service = _configProvider.GetService(resolvedConnectionString, resolvedHostName, resolvedUserName, resolvedPassword, resolvedPort, resolvedVirtualHost);

            return service.Model;
        }
    }
}
