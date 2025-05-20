// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal class RabbitMQClientBuilder(RabbitMQExtensionConfigProvider configProvider, IOptions<RabbitMQOptions> options) : IConverter<RabbitMQAttribute, IRabbitMQService>
{
    private readonly RabbitMQExtensionConfigProvider configProvider = configProvider;
    private readonly IOptions<RabbitMQOptions> options = options;

    public IRabbitMQService Convert(RabbitMQAttribute attribute)
    {
        return this.CreateModelFromAttribute(attribute);
    }

    private IRabbitMQService CreateModelFromAttribute(RabbitMQAttribute attribute)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        string resolvedConnectionString = Utility.FirstOrDefault(attribute.ConnectionStringSetting, this.options.Value.ConnectionString);
        bool resolvedDisableCertificateValidation = Utility.FirstOrDefault(attribute.DisableCertificateValidation, this.options.Value.DisableCertificateValidation);

        return this.configProvider.GetService(resolvedConnectionString, resolvedDisableCertificateValidation);
    }
}
