// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;

namespace WebJobs.Extensions.RabbitMQ.EndToEnd
{
    public class SamplesTypeLocator : ITypeLocator
    {
        private Type[] types;

        public SamplesTypeLocator(params Type[] types)
        {
            this.types = types;
        }

        public IReadOnlyList<Type> GetTypes()
        {
            return this.types;
        }
    }
}