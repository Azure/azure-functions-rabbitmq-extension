#nullable enable
namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Trigger
{
    public interface IRabbitMQQueueDefinitionFactory
    {
        public RabbitMQQueueDefinition BuildDefinition(string queueName);

        public string? GetDeadLetterQueueName(string queueName);

        public RabbitMQQueueDefinition BuildDeadLetterQueueDefinition(string deadLetterQueueName);
    }
}