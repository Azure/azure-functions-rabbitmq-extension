// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extension methods for RabbitMQ integration.
    /// </summary>
    public static class RabbitMQWebJobsBuilderExtensions
    {
        /// <summary>
        /// Adds the RabbitMQ extension to the provided <see cref="IWebJobsBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
        public static IWebJobsBuilder AddRabbitMQ(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddExtension<RabbitMQExtensionConfigProvider>()
                .ConfigureOptions<RabbitMQOptions>((config, path, options) =>
                {
                    options.ConnectionString = config.GetWebJobsConnectionString(Constants.RabbitMQ);
                    IConfigurationSection section = config.GetSection(path);
                    section.Bind(options);
                });

            builder.Services.AddSingleton<IRabbitMQServiceFactory, DefaultRabbitMQServiceFactory>();
            return builder;
        }

        /// <summary>
        /// Adds the RabbitMQ extension to the provided <see cref="IWebJobsBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
        /// <param name="configure">An <see cref="Action{RabbitMQOptions}"/> to configure the provided <see cref="RabbitMQOptions"/>.</param>
        public static IWebJobsBuilder AddRabbitMQ(this IWebJobsBuilder builder, Action<RabbitMQOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddRabbitMQ();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
