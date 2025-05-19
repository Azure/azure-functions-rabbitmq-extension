// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal class DefaultRabbitMQServiceFactory : IRabbitMQServiceFactory
{
    public IRabbitMQService CreateService(string connectionString, string queueName, bool disableCertificateValidation, ILogger logger)
    {
        return new RabbitMQService(connectionString, queueName, disableCertificateValidation, logger);
    }

    public IRabbitMQService CreateService(string connectionString, bool disableCertificateValidation, ILogger logger)
    {
        return new RabbitMQService(connectionString, disableCertificateValidation, logger);
    }
}
