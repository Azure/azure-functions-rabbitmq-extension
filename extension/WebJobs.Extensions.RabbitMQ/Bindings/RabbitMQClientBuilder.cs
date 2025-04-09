// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal class RabbitMQClientBuilder(RabbitMQExtensionConfigProvider configProvider, IOptions<RabbitMQOptions> options) : IConverter<RabbitMQAttribute, IModel>
{
    private readonly RabbitMQExtensionConfigProvider configProvider = configProvider;
    private readonly IOptions<RabbitMQOptions> options = options;

    public IModel Convert(RabbitMQAttribute attribute)
    {
        return this.CreateModelFromAttribute(attribute);
    }

    private IModel CreateModelFromAttribute(RabbitMQAttribute attribute)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        string resolvedConnectionString = Utility.FirstOrDefault(attribute.ConnectionStringSetting, this.options.Value.ConnectionString);
        bool resolvedDisableCertificateValidation = Utility.FirstOrDefault(attribute.DisableCertificateValidation, this.options.Value.DisableCertificateValidation);

        IRabbitMQService service = this.configProvider.GetService(resolvedConnectionString, resolvedDisableCertificateValidation);

        return service.Model;
    }
}
