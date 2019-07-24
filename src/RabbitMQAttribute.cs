// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Attribute used to bind a parameter to a RabbitMQMessage that will automatically be
    /// sent when the function completes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]

    public sealed class RabbitMQAttribute : Attribute
    {
        // Necessary for creating connections
        [AutoResolve]
        public string Hostname { get; set; }

        // Necessary for sending and receiving messages
        // Settings for creating and sending to/receiving from a queue.
        [AutoResolve]
        public string QueueName { get; set; }
    }
}
