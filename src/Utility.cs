// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions
{
    internal static class Utility
    {
        internal static string FirstOrDefault(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
        }

        internal static int FirstOrDefault(params int[] values)
        {
            return values.FirstOrDefault(v =>
            {
                if (v != 0)
                {
                    return true;
                }

                return false;
            });
        }

        internal static bool ValidateUserNamePassword(string userName, string password, string hostName)
        {
            if (!hostName.Equals(Constants.LocalHost, StringComparison.InvariantCultureIgnoreCase) && (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)))
            {
                return false;
            }

            return true;
        }

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
            catch (Exception)
            {
                // Do Nothing
            }

            return optionsConnectionString;
        }
    }
}
