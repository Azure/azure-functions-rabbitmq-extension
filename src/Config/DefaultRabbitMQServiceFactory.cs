// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class DefaultRabbitMQServiceFactory : IRabbitMQServiceFactory
    {
        public IRabbitMQService CreateService(string connectionString, string queueName, string exchangeName, string hostName,  string userName, string password, int port)
        {
            return new RabbitMQService(connectionString, queueName, exchangeName, hostName, userName, password, port);
        }

        public IRabbitMQService CreateService(string connectionString, string hostName, string userName, string password, int port)
        {
            return new RabbitMQService(connectionString, hostName, userName, password, port);
        }
    }
}
