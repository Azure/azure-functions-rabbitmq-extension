// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

public interface IRabbitMQServiceFactory
{
    IRabbitMQService CreateService(string connectionString, string queueName, bool disableCertificateValidation, ILogger logger);

    IRabbitMQService CreateService(string connectionString, bool disableCertificateValidation, ILogger logger);
}
