// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;

namespace WebJobs.Extensions.RabbitMQ.Samples
{
    public class SamplesTypeLocator : ITypeLocator
    {
        private readonly Type[] _types;

        public SamplesTypeLocator(params Type[] types)
        {
            _types = types;
        }

        public IReadOnlyList<Type> GetTypes()
        {
            return _types;
        }
    }
}