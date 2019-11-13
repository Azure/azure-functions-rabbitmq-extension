// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions
{
    internal static class Utility
    {
        internal static string FirstOrDefault(params string[] values) => values.FirstOrDefault(v => !string.IsNullOrEmpty(v));

        internal static bool FirstOrDefault(params bool[] values) => values.FirstOrDefault(v => v);

        internal static int FirstOrDefault(params int[] values) => values.FirstOrDefault(v => v != 0);

        internal static bool ValidateUserNamePassword(string userName, string password, string hostName) => hostName.Equals(Constants.LocalHost, StringComparison.InvariantCultureIgnoreCase) || (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password));

        internal static string ResolveConnectionString(string attributeConnectionStringKey, string optionsConnectionString, IConfiguration configuration)
        {
            try
            {
                string resolvedString = configuration.GetConnectionStringOrSetting(attributeConnectionStringKey);
                if (!string.IsNullOrEmpty(resolvedString))
                {
                    return resolvedString;
                }
            }
            catch
            {}

            return optionsConnectionString;
        }
    }
}
