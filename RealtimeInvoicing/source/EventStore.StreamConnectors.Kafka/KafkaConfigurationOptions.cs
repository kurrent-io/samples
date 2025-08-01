namespace EventStore.StreamConnectors.Kafka {
    using EventStore.StreamConnectors.RDBMS;

    public class KafkaConfigurationOptions : StreamConfigurationOptions, IKafkaOptions {
        /// <summary>
        /// The "group" as setup in EventStore which identifies this subscriber.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// In kafka, where are we publishing the events from eventstore "to"
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// This is the key/value pair to setup a kafka producer connection.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// When the subscription to EventStore is being stopped, this is the wait time for the stop.
        /// </summary>
        public TimeSpan DisconnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }
}
