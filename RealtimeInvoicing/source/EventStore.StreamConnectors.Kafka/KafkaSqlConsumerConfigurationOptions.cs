namespace EventStore.StreamConnectors.Kafka {
    using System.Collections.Generic;

    using EventStore.StreamConnectors.RDBMS;

    public class KafkaSqlConsumerConfigurationOptions : KafkaConfigurationOptions, ISqlOptions {
        public string Namespace { get; set; } = "dbo";
        public string Table { get; set; }
        public IEnumerable<ColumnMap> Columns { get; set; }
        public IQueryFormatter QueryFormatter { get; set; }
    }
}
