// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class RabbitMQTriggerBinding : ITriggerBinding
    {
        private readonly IRabbitMQService _service;
        private readonly string _queueName;
        private readonly string _hostName;
        private readonly ushort _batchNumber;
        private readonly ILogger _logger;

        public RabbitMQTriggerBinding(IRabbitMQService service, string hostname, string queueName, ushort batchNumber, ILogger logger)
        {
            _service = service;
            _queueName = queueName;
            _hostName = hostname;
            _batchNumber = batchNumber;
            _logger = logger;
            BindingDataContract = CreateBindingDataContract();
        }

        public Type TriggerValueType
        {
            get
            {
                return typeof(BasicDeliverEventArgs);
            }
        }

        public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>();

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            BasicDeliverEventArgs message = value as BasicDeliverEventArgs;

            IReadOnlyDictionary<string, object> bindingData = CreateBindingData(message);
            return Task.FromResult<ITriggerData>(new TriggerData(null, bindingData));
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            return Task.FromResult<IListener>(new RabbitMQListener(context.Executor, _service, _queueName, _batchNumber, _logger));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new RabbitMQTriggerParameterDescriptor
            {
                Hostname = _hostName,
                QueueName = _queueName,
            };
        }

        internal static IReadOnlyDictionary<string, Type> CreateBindingDataContract()
        {
            var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            contract.Add("ConsumerTag", typeof(string));
            contract.Add("DeliveryTag", typeof(ulong));
            contract.Add("Redelivered", typeof(bool));
            contract.Add("Exchange", typeof(string));
            contract.Add("RoutingKey", typeof(string));
            contract.Add("BasicProperties", typeof(IBasicProperties));
            contract.Add("Body", typeof(byte[]));

            return contract;
        }

        internal static IReadOnlyDictionary<string, object> CreateBindingData(BasicDeliverEventArgs value)
        {
            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            SafeAddValue(() => bindingData.Add(nameof(value.ConsumerTag), value.ConsumerTag));
            SafeAddValue(() => bindingData.Add(nameof(value.DeliveryTag), value.DeliveryTag));
            SafeAddValue(() => bindingData.Add(nameof(value.Redelivered), value.Redelivered));
            SafeAddValue(() => bindingData.Add(nameof(value.Exchange), value.Exchange));
            SafeAddValue(() => bindingData.Add(nameof(value.RoutingKey), value.RoutingKey));
            SafeAddValue(() => bindingData.Add(nameof(value.BasicProperties), value.BasicProperties));
            SafeAddValue(() => bindingData.Add(nameof(value.Body), value.Body));

            return bindingData;
        }

        private static void SafeAddValue(Action addValue)
        {
            try
            {
                addValue();
            }
            catch
            {
                // some message property getters can throw, based on the
                // state of the message
            }
        }
    }
}
