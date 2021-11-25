// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebJobs.Extensions.RabbitMQ.Samples
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // Add or remove types from this list to choose which functions will
            // be indexed by the JobHost.
            // To run some of the other samples included, add their types to this list
            var typeLocator = new SamplesTypeLocator(
                typeof(RabbitMQSamples));

            var builder = new HostBuilder()
               .UseEnvironment("Development")
               .ConfigureWebJobs(webJobsBuilder =>
               {
                   webJobsBuilder
                   .AddAzureStorageCoreServices()
                   .AddAzureStorageBlobs()
                   .AddAzureStorageQueues()
                   .AddRabbitMQ()
                   .AddTimers();
               })
               .ConfigureLogging(b =>
               {
                   b.SetMinimumLevel(LogLevel.Debug);
                   b.AddConsole();
               })
               .ConfigureServices(s =>
               {
                   s.AddSingleton<ITypeLocator>(typeLocator);
               })
               .UseConsoleLifetime();

            var host = builder.Build();
            using (host)
            {
                var jobHost = (JobHost)host.Services.GetService<IJobHost>();

                await host.RunAsync();
            }
        }
    }
}