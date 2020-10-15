#nullable enable
using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.Trigger
{
    public enum QueueType
    {
        Classic,
        Quorum,
    }

    public enum OverflowBehaviour
    {
        DropHead,
        RejectPublish,
        RejectPublishDlx,
    }

    public class RabbitMQQueueDefinitionBuilder
    {
        private readonly QueueType _queueType;
        private bool _durable;
        private bool _autoDelete;
        private bool _exclusive;
        private int? _queueExpiresMilliseconds;
        private int? _messageTtlMilliseconds;
        private int? _maxLength;
        private int? _maxLengthByte;
        private OverflowBehaviour? _overflowBehaviour;
        private string? _deadLetterExchangeName;
        private string? _deadLetterRoutingKey;
        private bool? _singleActiveConsumer;
        private byte? _maxPriorityLevel;
        private bool? _lazyMode;
        private string? _masterLocator;

        public RabbitMQQueueDefinitionBuilder(QueueType queueType)
        {
            _queueType = queueType;
        }

        /// <summary>
        /// Define the queue to be Durable
        /// Durable queues will be recovered on node boot, including messages in them published as persistent.
        /// Messages published as transient will be discarded during recovery, even if they were stored in durable queues
        /// <see href="https://www.rabbitmq.com/queues.html"/>
        /// </summary>
        public RabbitMQQueueDefinitionBuilder Durable()
        {
            _durable = true;
            return this;
        }

        /// <summary>
        /// Define the queue to be Transient
        /// Transient queues will be deleted on node boot. They therefore will not survive a node restart, by design.
        /// Messages in transient queues will also be discarded.
        /// <see href="https://www.rabbitmq.com/queues.html"/>
        /// </summary>
        public RabbitMQQueueDefinitionBuilder Transient()
        {
            _durable = false;
            return this;
        }

        /// <summary>
        /// An auto-delete queue will be deleted when its last consumer is cancelled (by calling .Cancel) or gone
        /// (closed channel or connection, or lost TCP connection with the server).
        /// <see href="https://www.rabbitmq.com/queues.html"/>
        /// </summary>
        public RabbitMQQueueDefinitionBuilder AutoDelete(bool autoDelete = true)
        {
            _autoDelete = autoDelete;
            return this;
        }

        /// <summary>
        /// An exclusive queue can only be used (consumed from, purged, deleted, etc) by its declaring connection.
        /// <see href="https://www.rabbitmq.com/queues.html"/>
        /// </summary>
        public RabbitMQQueueDefinitionBuilder Exclusive(bool exclusive = true)
        {
            _exclusive = exclusive;
            return this;
        }

        /// <summary>
        /// How long a queue can be unused for before it is automatically deleted (milliseconds)
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithExpire(int expiresMilliseconds)
        {
            _queueExpiresMilliseconds = expiresMilliseconds;
            return this;
        }

        /// <summary>
        /// How long a message published to a queue can live before it is discarded (milliseconds).
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithMessageTtl(int ttlMilliseconds)
        {
            _messageTtlMilliseconds = ttlMilliseconds;
            return this;
        }

        /// <summary>
        /// How long a message published to a queue can live before it is discarded (milliseconds).
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithMessageTtl(TimeSpan ttl)
        {
            _messageTtlMilliseconds = (int)ttl.TotalMilliseconds;
            return this;
        }

        /// <summary>
        /// How many (ready) messages a queue can contain before it starts to drop them from its head.
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithMaxLength(int maxLength)
        {
            _maxLength = maxLength;
            return this;
        }

        /// <summary>
        /// Total body size for ready messages a queue can contain before it starts to drop them from its head.
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithMaxLengthByte(int maxLengthByte)
        {
            _maxLengthByte = maxLengthByte;
            return this;
        }

        /// <summary>
        /// Sets the queue overflow behaviour. This determines what happens to messages when the maximum length of a
        /// queue is reached. Valid values are drop-head, reject-publish or reject-publish-dlx. The quorum queue type only supports drop-head.
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithOverflowBehaviour(OverflowBehaviour overflowBehaviour)
        {
            if (_queueType == QueueType.Quorum && overflowBehaviour != OverflowBehaviour.DropHead)
            {
                throw new ArgumentException("The quorum queue type only supports drop-head.", nameof(overflowBehaviour));
            }

            _overflowBehaviour = overflowBehaviour;
            return this;
        }

        /// <summary>
        /// Optional name of an exchange to which messages will be republished if they are rejected or expire.
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithDeadLetterExchange(string exchangeName)
        {
            _deadLetterExchangeName = exchangeName;
            return this;
        }

        /// <summary>
        /// Optional replacement routing key to use when a message is dead-lettered.
        /// If this is not set, the message's original routing key will be used.
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithDeadLetterRoutingKey(string routingKey)
        {
            _deadLetterRoutingKey = routingKey;
            return this;
        }

        /// <summary>
        /// If set, makes sure only one consumer at a time consumes from the queue and fails over to another registered
        /// consumer in case the active one is cancelled or dies.
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithSingleActiveConsumer()
        {
            _singleActiveConsumer = true;
            return this;
        }

        /// <summary>
        /// Maximum number of priority levels for the queue to support;
        /// if not set, the queue will not support message priorities.
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithMaximumNumberOfPriorityLevel(byte maxPriorityLevel)
        {
            _maxPriorityLevel = maxPriorityLevel;
            return this;
        }

        /// <summary>
        /// Set the queue into lazy mode, keeping as many messages as possible on disk to reduce RAM usage;
        /// if not set, the queue will keep an in-memory cache to deliver messages as fast as possible.
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithLazyMode()
        {
            _lazyMode = true;
            return this;
        }

        /// <summary>
        /// Set the queue into master location mode, determining the rule by which the queue master is located when
        /// declared on a cluster of nodes.
        /// </summary>
        public RabbitMQQueueDefinitionBuilder WithMasterLocator(string locator)
        {
            _masterLocator = locator;
            return this;
        }

        public RabbitMQQueueDefinition Build()
        {
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            if (_queueExpiresMilliseconds != null)
            {
                arguments[Headers.XExpires] = _queueExpiresMilliseconds;
            }

            if (_messageTtlMilliseconds != null)
            {
                arguments[Headers.XMessageTTL] = _messageTtlMilliseconds;
            }

            if (_maxLength != null)
            {
                arguments[Headers.XMaxLength] = _maxLength;
            }

            if (_maxLengthByte != null)
            {
                arguments[Headers.XMaxLengthInBytes] = _maxLengthByte;
            }

            if (_overflowBehaviour != null)
            {
                // TODO: After client version 6+: Headers.XOverflow
                arguments["x-overflow"] = _overflowBehaviour.Value switch
                {
                    OverflowBehaviour.DropHead => "drop-head",
                    OverflowBehaviour.RejectPublish => "reject-publish",
                    OverflowBehaviour.RejectPublishDlx => "reject-publish-dlx",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            if (_deadLetterExchangeName != null)
            {
                arguments[Headers.XDeadLetterExchange] = _deadLetterExchangeName;
            }

            if (_deadLetterRoutingKey != null)
            {
                arguments[Headers.XDeadLetterRoutingKey] = _deadLetterRoutingKey;
            }

            // TODO: After client version 6+: Headers.XSingleActiveConsumer
            if (_singleActiveConsumer != null)
            {
                arguments["x-single-active-consumer"] = _singleActiveConsumer;
            }

            if (_maxPriorityLevel != null)
            {
                arguments[Headers.XMaxPriority] = _maxPriorityLevel;
            }

            // TODO: After client version 6+: Headers.XQueueMode
            if (_lazyMode != null)
            {
                arguments["x-queue-mode"] = _lazyMode.Value ? "lazy" : "default";
            }

            if (_masterLocator != null)
            {
                arguments["x-queue-master-locator"] = _masterLocator;
            }

            return new RabbitMQQueueDefinition
            {
                Durable = _durable,
                AutoDelete = _autoDelete,
                Exclusive = _exclusive,
                Arguments = arguments.Count == 0 ? null : arguments,
            };
        }
    }
}