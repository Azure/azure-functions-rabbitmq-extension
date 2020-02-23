namespace Microsoft.Azure.WebJobs.Extensions
{
    internal static class Constants
    {
        public const string LocalHost = "localhost";
        public const string RabbitMQ = "RabbitMQ";
        public const string RequeueCount = "requeueCount";
        public const string DeadLetterExchangeKey = "x-dead-letter-exchange";
        public const string DefaultDLXSetting = "direct";
        public const string DeadLetterRoutingKey = "x-dead-letter-routing-key";
        public const string DeadLetterRoutingKeyValue = "poison-queue";
        public const string DefaultUsername = "guest";
        public const string DefaultPassword = "guest";
        public const int DefaultPort = 5672;
        public const ushort PrefetchCountDefault = 8;
    }
}
