// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

public class RabbitMQMessageActions
{
    private readonly IRabbitMQService service;
    private readonly BasicDeliverEventArgs message;

    internal RabbitMQMessageActions(IRabbitMQService service, BasicDeliverEventArgs message)
    {
        this.service = service;
        this.message = message;
    }

    public async Task Reject(bool requeue = false)
    {
        await Task.Run(() =>
        {
            this.service.Reject(deliveryTag: this.message.DeliveryTag, requeue, logDetails: $"ConsumerTag: {this.message.ConsumerTag}");
        });
    }

    public async Task Acknowledge()
    {
        await Task.Run(() =>
        {
            this.service.Acknowledge(deliveryTag: this.message.DeliveryTag, multiple: false, logDetails: $"ConsumerTag: {this.message.ConsumerTag}");
        });
    }
}
