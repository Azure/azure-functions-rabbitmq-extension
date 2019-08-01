using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal interface IRabbitMQService
    {
        IModel GetChannel();
    }
}
