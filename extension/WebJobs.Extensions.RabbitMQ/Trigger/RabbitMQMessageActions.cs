// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.WebJobs.Host.Scale;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

public class RabbitMQMessageActions
{
    private readonly IModel channel;

    internal RabbitMQMessageActions(IModel channel)
    {
        this.channel = channel;
    }

    public void BasicReject(ulong deliveryTag, bool requeue = false)
    {
        this.channel.BasicReject(deliveryTag, requeue: requeue);
    }

    public void BasicAck(ulong deliveryTag, bool multiple = false)
    {
        this.channel.BasicAck(deliveryTag, multiple: multiple);
    }
}
