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

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal class RabbitMQTriggerBinding : ITriggerBinding
{
    private readonly IRabbitMQService service;
    private readonly ILogger logger;
    private readonly Type parameterType;
    private readonly string queueName;
    private readonly ushort prefetchCount;

    public RabbitMQTriggerBinding(IRabbitMQService service, string queueName, ILogger logger, Type parameterType, ushort prefetchCount)
    {
        this.service = service;
        this.queueName = queueName;
        this.logger = logger;
        this.parameterType = parameterType;
        this.prefetchCount = prefetchCount;
        this.BindingDataContract = CreateBindingDataContract();
    }

    public Type TriggerValueType => typeof(BasicDeliverEventArgs);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>();

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        var message = (BasicDeliverEventArgs)value;
        IReadOnlyDictionary<string, object> bindingData = CreateBindingData(message);

        return Task.FromResult<ITriggerData>(new TriggerData(new BasicDeliverEventArgsValueProvider(message, this.parameterType), bindingData));
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context), "Missing listener context");

        return Task.FromResult<IListener>(new RabbitMQListener(
            context.Executor,
            this.service.Model,
            this.logger,
            context.Descriptor.Id,
            this.queueName,
            this.prefetchCount));
    }

    public ParameterDescriptor ToParameterDescriptor()
    {
        return new RabbitMQTriggerParameterDescriptor
        {
            QueueName = this.queueName,
        };
    }

    internal static IReadOnlyDictionary<string, Type> CreateBindingDataContract()
    {
        var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConsumerTag"] = typeof(string),
            ["DeliveryTag"] = typeof(ulong),
            ["Redelivered"] = typeof(bool),
            ["Exchange"] = typeof(string),
            ["RoutingKey"] = typeof(string),
            ["BasicProperties"] = typeof(IBasicProperties),
            ["Body"] = typeof(ReadOnlyMemory<byte>),
        };

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
        catch (ArgumentException)
        {
            // some message property getters can throw, based on the
            // state of the message
        }
    }
}
