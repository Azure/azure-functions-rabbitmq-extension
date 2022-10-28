// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal sealed class RabbitMQActivitySource
{
    private static readonly ActivitySource ActivitySource = new("Microsoft.Azure.WebJobs.Extensions.RabbitMQ");

    /// <summary>
    /// Creates a new activity.
    /// </summary>
    /// <param name="basicProperties">Represents the RabbitMQ message content header.</param>
    /// <returns>The created activity object.</returns>
    public static Activity StartActivity(IBasicProperties basicProperties)
    {
        // Ideally, we would have used string values for headers, but RabbitMQ client has an old quirk where it does
        // not differentiate between string headers and byte-array headers when decoding them. See:
        // https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/415. Hence, it is decided to also set a byte[]
        // as the value for 'traceparent' header to keep its types consistent for all cases.
        if (basicProperties.Headers?.ContainsKey("traceparent") == true)
        {
            byte[] traceParentIdInBytes = basicProperties.Headers["traceparent"] as byte[];
            string traceparentId = Encoding.UTF8.GetString(traceParentIdInBytes);
            return ActivitySource.StartActivity("Trigger", ActivityKind.Consumer, traceparentId);
        }
        else
        {
            Activity activity = ActivitySource.StartActivity("Trigger", ActivityKind.Consumer);

            // Method 'StartActivity' will return null if it has no event listeners.
            if (activity != null)
            {
                basicProperties.Headers ??= new Dictionary<string, object>();
                byte[] traceParentIdInBytes = Encoding.UTF8.GetBytes(activity.Id);
                basicProperties.Headers["traceparent"] = traceParentIdInBytes;
            }

            return activity;
        }
    }
}
