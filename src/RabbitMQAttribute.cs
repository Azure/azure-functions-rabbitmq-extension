// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Security;
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
        /// <summary>
        /// Gets or sets the HostName used to authenticate with RabbitMQ.
        /// This is ignored if 'ConnectionStringSetting' is set.
        /// </summary>
        [AutoResolve]
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the QueueName to send messages to.
        /// </summary>
        [AutoResolve]
        public string QueueName { get; set; }

        /// <summary>
        /// Gets or sets the name of the app setting containing the username to authenticate with RabbitMQ. Eg: { UserName: "UserNameFromSettings" }
        /// This is ignored if 'ConnectionStringSetting' is set.
        /// </summary>
        [AppSetting]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the name of the app setting containing the password to authenticate with RabbitMQ. Eg: { Password: "PasswordFromSettings" }
        ///  This is ignored if 'ConnectionStringSetting' is set.
        /// </summary>
        [AppSetting]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the Port used. Defaults to 0.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the name of app setting that contains the connection string to authenticate with RabbitMQ.
        /// </summary>
        [ConnectionString]
        public string ConnectionStringSetting { get; set; }

        /// <summary>
        /// Gets or sets the acceptable policy errors when connecting to RabbitMQ over TLS. Defaults to RemoteCertificateNameMismatch, which is the default for the .Net RabbitMQ client.
        /// </summary>
        public SslPolicyErrors AcceptablePolicyErrors { get; set; } = SslPolicyErrors.RemoteCertificateNameMismatch;
    }
}
