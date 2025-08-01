namespace RTI.KafkaConsumer.ToRDBMS {
    using System.Data.Common;

    using EventStore.ClientAPI;
    using EventStore.StreamConnectors.Kafka;

    using Microsoft.Extensions.Logging;

    internal class InvoiceHeaderConsumer : KafkaSqlStreamListener {
        public InvoiceHeaderConsumer(DbConnection connection, KafkaSqlConsumerConfigurationOptions options, IEventStoreConnection esConnection, ILoggerFactory loggerFactory) : base(connection, options, esConnection, loggerFactory) {
        }
    }
}
