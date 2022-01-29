// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public interface IRabbitMQServiceFactory
    {
        IRabbitMQService CreateService(string connectionString, string hostName, string queueName, string userName, string password, int port, string virtualHost);

        IRabbitMQService CreateService(string connectionString, string hostName, string userName, string password, int port, string virtualHost);
    }
}