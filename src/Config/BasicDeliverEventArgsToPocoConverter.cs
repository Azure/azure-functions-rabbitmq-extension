﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class BasicDeliverEventArgsToPocoConverter<T> : IConverter<BasicDeliverEventArgs, T>
    {
        private readonly ILogger _logger;

        public BasicDeliverEventArgsToPocoConverter(ILogger logger)
        {
            _logger = logger;
        }

        public T Convert(BasicDeliverEventArgs arg)
        {
            string body = Encoding.UTF8.GetString(arg.Body);
            JToken jsonObj = null;

            try
            {
                jsonObj = JToken.Parse(body);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed converting BasicDeliverEventArgs body to Poco");
                return default(T);
            }

            return jsonObj.ToObject<T>();
        }
    }
}
