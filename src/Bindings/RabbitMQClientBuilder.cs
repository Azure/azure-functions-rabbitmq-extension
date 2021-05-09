// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Security;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class RabbitMQClientBuilder : IConverter<RabbitMQAttribute, IModel>
    {
        private readonly RabbitMQExtensionConfigProvider _configProvider;
        private readonly IOptions<RabbitMQOptions> _options;
        private readonly IDictionary<RabbitMQAttribute, IModel> _rabbitMQAttributeToModel;

        public RabbitMQClientBuilder(RabbitMQExtensionConfigProvider configProvider, IOptions<RabbitMQOptions> options)
        {
            _configProvider = configProvider;
            _options = options;
            _rabbitMQAttributeToModel = new Dictionary<RabbitMQAttribute, IModel>();
        }

        public IModel Convert(RabbitMQAttribute attribute)
        {
            return GetOrCreateModel(attribute);
        }

        private IModel CreateModelFromAttribute(RabbitMQAttribute attribute)
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

            SslPolicyErrors acceptablePolicyErrors = attribute.AcceptablePolicyErrors;

            IRabbitMQService service = _configProvider.GetService(resolvedConnectionString, resolvedHostName, resolvedUserName, resolvedPassword, resolvedPort, acceptablePolicyErrors);

            return service.Model;
        }

        private IModel GetOrCreateModel(RabbitMQAttribute attribute)
        {
            if (!_rabbitMQAttributeToModel.TryGetValue(attribute, out IModel rabbitMQModel))
            {
                rabbitMQModel = CreateModelFromAttribute(attribute);
                _rabbitMQAttributeToModel.Add(attribute, rabbitMQModel);
            }

            return rabbitMQModel;
        }
    }
}