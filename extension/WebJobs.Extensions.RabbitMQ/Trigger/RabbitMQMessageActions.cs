// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.WebJobs.Host.Scale;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

public class RabbitMQMessageActions
{
    private readonly IRabbitMQService service;

    internal RabbitMQMessageActions(IRabbitMQService service)
    {
        this.service = service;
    }

    public void BasicReject(ulong deliveryTag, bool requeue = false)
    {
        this.service.Reject(deliveryTag, requeue, logDetails: string.Empty, throwOnMissing: true);
    }

    public void BasicAck(ulong deliveryTag, bool multiple = false)
    {
        this.service.Acknowledge(deliveryTag, multiple, logDetails: string.Empty, throwOnMissing: true);
    }

    public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
    {
        this.service.Publish(exchange, routingKey, basicProperties, body);
    }

    public BasicGetResult BasicGet(string queue, bool autoAck)
    {
        return this.service.Get(queue, autoAck);
    }
}
