using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMQService(string hostname)
        {
            _connectionFactory = new ConnectionFactory() { HostName = hostname };
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public IModel GetChannel()
        {
            return _channel;
        }
    }
}
