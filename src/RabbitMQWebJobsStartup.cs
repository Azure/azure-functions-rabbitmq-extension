// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Hosting;

[assembly: WebJobsStartup(typeof(RabbitMQWebJobsStartup))]

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    public class RabbitMQWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddRabbitMQ();
        }
    }
}
