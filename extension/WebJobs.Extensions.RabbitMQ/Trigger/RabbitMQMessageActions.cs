// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

public class RabbitMQMessageActions
{
    private readonly IRabbitMQService service;

    internal RabbitMQMessageActions(IRabbitMQService service)
    {
        this.service = service;
    }

    public async Task BasicReject(ulong deliveryTag, bool requeue = false)
    {
        await Task.Run(() =>
        {
            this.service.Reject(deliveryTag, requeue, logDetails: string.Empty, throwOnMissing: true);
        });
    }

    public async Task BasicAck(ulong deliveryTag, bool multiple = false)
    {
        await Task.Run(() =>
        {
            this.service.Acknowledge(deliveryTag, multiple, logDetails: string.Empty, throwOnMissing: true);
        });
    }

    public async Task BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
    {
        await Task.Run(() =>
        {
            this.service.Publish(exchange, routingKey, basicProperties, body);
        });
    }

    public async Task<BasicGetResult> BasicGet(string queue, bool autoAck)
    {
        return await Task.Run(() =>
        {
            return this.service.Get(queue, autoAck);
        });
    }
}
