namespace RTI.Writers.Kafka {

    using EventStore.ClientAPI;
    using EventStore.StreamConnectors.Kafka;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Note: These are basically service locator classes.  the way that a generic host works is that you cannot
    /// create multiple instances of the same service host.
    /// </summary>

    internal class MongoStreamProcessor : KafkaStreamProcessor {
        public MongoStreamProcessor(KafkaConfigurationOptions options, IEventStoreConnection connection, ILoggerFactory loggerFactory) : base(options, connection, loggerFactory) {
        }
    }

    internal class RedisStreamProcessor : KafkaStreamProcessor {
        public RedisStreamProcessor(KafkaConfigurationOptions options, IEventStoreConnection connection, ILoggerFactory loggerFactory) : base(options, connection, loggerFactory) {
        }
    }

    internal class InvoiceHeaderSqlStreamProcessor : KafkaStreamProcessor {
        public InvoiceHeaderSqlStreamProcessor(KafkaConfigurationOptions options, IEventStoreConnection connection, ILoggerFactory loggerFactory) : base(options, connection, loggerFactory) {
        }
    }

    internal class InvoiceItemSqlStreamProcessor : KafkaStreamProcessor {
        public InvoiceItemSqlStreamProcessor(KafkaConfigurationOptions options, IEventStoreConnection connection, ILoggerFactory loggerFactory) : base(options, connection, loggerFactory) {
        }
    }

    internal class InvoicePaymentSqlStreamProcessor : KafkaStreamProcessor {
        public InvoicePaymentSqlStreamProcessor(KafkaConfigurationOptions options, IEventStoreConnection connection, ILoggerFactory loggerFactory) : base(options, connection, loggerFactory) {
        }
    }
}
