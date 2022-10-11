// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal class DefaultRabbitMQServiceFactory : IRabbitMQServiceFactory
{
    public IRabbitMQService CreateService(string connectionString, string queueName, bool disableCertificateValidation)
    {
        return new RabbitMQService(connectionString, queueName, disableCertificateValidation);
    }

    public IRabbitMQService CreateService(string connectionString, bool disableCertificateValidation)
    {
        return new RabbitMQService(connectionString, disableCertificateValidation);
    }
}
