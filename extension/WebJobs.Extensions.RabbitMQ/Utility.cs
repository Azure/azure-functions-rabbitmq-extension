// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ;

internal static class Utility
{
    internal static string FirstOrDefault(params string[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
    }

    internal static TValue FirstOrDefault<TValue>(params TValue[] values)
        where TValue : IEquatable<TValue>
    {
        return values.FirstOrDefault(v => !v.Equals(default));
    }

    internal static string ResolveConnectionString(string attributeConnectionStringKey, string optionsConnectionString, IConfiguration configuration)
    {
        // Is the connection-string key present in the binding attribute?
        if (attributeConnectionStringKey != null)
        {
            string resolvedString = configuration.GetConnectionStringOrSetting(attributeConnectionStringKey);

            // Is there a value associated with the key?
            if (!string.IsNullOrEmpty(resolvedString))
            {
                return resolvedString;
            }
        }

        return optionsConnectionString;
    }
}
