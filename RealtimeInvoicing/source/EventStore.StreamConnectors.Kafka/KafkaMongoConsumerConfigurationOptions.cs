namespace EventStore.StreamConnectors.Kafka {
    public class KafkaMongoConsumerConfigurationOptions : KafkaConfigurationOptions {
        public string CollectionName { get; set; }
    }
}
