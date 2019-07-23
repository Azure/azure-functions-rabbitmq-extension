using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Listeners
{
    internal class RabbitMQTriggerObserver
    {
        private readonly ITriggeredFunctionExecutor executor;
        private readonly EventingBasicConsumer consumer;

        public RabbitMQTriggerObserver(ITriggeredFunctionExecutor executor, EventingBasicConsumer consumer)
        {
            this.executor = executor;
            this.consumer = consumer;
        }

        public Task ProcessChangesAsync(CancellationToken cancellationToken)
        {
            this.consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);

                // figure out where to input this lol
                this.executor.TryExecuteAsync(new TriggeredFunctionData() { TriggerValue = message }, cancellationToken);
            };

            return Task.CompletedTask;
        }
    }
}
