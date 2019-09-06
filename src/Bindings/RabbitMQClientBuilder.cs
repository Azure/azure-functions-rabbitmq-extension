using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Bindings
{
    internal class RabbitMQClientBuilder : IConverter<RabbitMQAttribute, IRabbitMQModel>
    {
        private readonly RabbitMQExtensionConfigProvider _configProvider;

        public RabbitMQClientBuilder(RabbitMQExtensionConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        IRabbitMQModel IConverter<RabbitMQAttribute, IRabbitMQModel>.Convert(RabbitMQAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            string resolvedConnectionString = _configProvider.ResolveConnectionString(attribute.ConnectionStringSetting);
            IRabbitMQService service = _configProvider.GetService(resolvedConnectionString, string.Empty, string.Empty, string.Empty, string.Empty, 0, string.Empty);

            return service.Model;
        }
    }
}
