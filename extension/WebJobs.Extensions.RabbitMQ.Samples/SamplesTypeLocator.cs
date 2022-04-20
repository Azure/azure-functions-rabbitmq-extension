// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Samples
{
    public class SamplesTypeLocator : ITypeLocator
    {
        private readonly Type[] types;

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
