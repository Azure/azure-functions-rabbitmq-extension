namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class RabbitMQMessage
    {
        public RabbitMQMessage(byte[] body)
        {
            Body = body;
        }

        public byte[] Body { get; set; }

        public string RoutingKey { get; set; }
    }
}
