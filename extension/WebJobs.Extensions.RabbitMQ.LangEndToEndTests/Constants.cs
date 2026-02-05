// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.LangEndToEndTests;

/// <summary>
/// Constants used in Lang E2E tests for RabbitMQ binding.
/// </summary>
public static class Constants
{
    /// <summary>
    /// RabbitMQ connection settings.
    /// </summary>
    public static class RabbitMQ
    {
        public const string Host = "localhost";
        public const int Port = 5672;
        public const int ManagementPort = 15672;
        public const string User = "guest";
        public const string Password = "guest";
        public const string VirtualHost = "/";
        public static string ConnectionString => $"amqp://{User}:{Password}@{Host}:{Port}";
    }

    /// <summary>
    /// Azurite (Azure Storage Emulator) connection settings.
    /// </summary>
    public static class Azurite
    {
        public const string Host = "localhost";
        public const int BlobPort = 10000;
        public const int QueuePort = 10001;
        public const int TablePort = 10002;
        public const string AccountName = "devstoreaccount1";
        public const string AccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
        public static string ConnectionString => $"DefaultEndpointsProtocol=http;AccountName={AccountName};AccountKey={AccountKey};BlobEndpoint=http://{Host}:{BlobPort}/{AccountName};QueueEndpoint=http://{Host}:{QueuePort}/{AccountName};TableEndpoint=http://{Host}:{TablePort}/{AccountName};";
    }

    /// <summary>
    /// Function app endpoints.
    /// </summary>
    public static class FunctionApps
    {
        public static class Java
        {
            public const string BaseUrl = "http://localhost:7071";
            public const string HttpTriggerRabbitMQOutput = $"{BaseUrl}/api/HttpTriggerRabbitMQOutput";
        }

        public static class Python
        {
            public const string BaseUrl = "http://localhost:7072";
            public const string HttpTriggerRabbitMQOutput = $"{BaseUrl}/api/HttpTriggerRabbitMQOutput";
        }
    }

    /// <summary>
    /// Queue names used in tests.
    /// </summary>
    public static class Queues
    {
        public const string InputQueue = "test-input-queue";
        public const string ResultQueue = "test-result-queue";
    }

    /// <summary>
    /// Timeout settings.
    /// </summary>
    public static class Timeouts
    {
        public static readonly TimeSpan DockerComposeStartup = TimeSpan.FromMinutes(3);
        public static readonly TimeSpan ServiceHealthCheck = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan MessageWait = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan HttpRequest = TimeSpan.FromSeconds(30);
    }
}
