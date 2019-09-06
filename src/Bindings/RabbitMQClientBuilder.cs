using System;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Bindings
{
    internal class RabbitMQClientBuilder : IConverter<RabbitMQAttribute, IModel>
    {
        private readonly RabbitMQExtensionConfigProvider _configProvider;

        public RabbitMQClientBuilder(RabbitMQExtensionConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        IModel IConverter<RabbitMQAttribute, IModel>.Convert(RabbitMQAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            string resolvedConnectionString = _configProvider.ResolveAttribute(attribute.ConnectionStringSetting, "connectionStringSetting");
            string resolvedHostName = _configProvider.ResolveAttribute(attribute.HostName, "hostName");
            string resolvedQueueName = _configProvider.ResolveAttribute(attribute.QueueName, "queueName");
            string resolvedUserName = _configProvider.ResolveAttribute(attribute.UserName, "userName");
            string resolvedPassword = _configProvider.ResolveAttribute(attribute.Password, "password");
            int resolvedPort = _configProvider.ResolvePortNumber(attribute.Port);

            IRabbitMQService service = _configProvider.GetService(resolvedConnectionString, resolvedHostName, resolvedQueueName, resolvedUserName, resolvedPassword, resolvedPort, string.Empty);

            return service.Model;
        }
    }
}
