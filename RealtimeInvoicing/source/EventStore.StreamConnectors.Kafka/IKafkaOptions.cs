namespace EventStore.StreamConnectors.Kafka {
    using System;

    public interface IKafkaOptions {
        string Stream { get; set; }
        /// <summary>
        /// The "group" as setup in EventStore which identifies this subscriber.
        /// </summary>
        string Group { get; set; }

        /// <summary>
        /// In kafka, where are we publishing the events from eventstore "to"
        /// </summary>
        string Topic { get; set; }

        /// <summary>
        /// This is the key/value pair to setup a kafka producer connection.
        /// </summary>
        string ConnectionString { get; set; }

        /// <summary>
        /// When the subscription to EventStore is being stopped, this is the wait time for the stop.
        /// </summary>
        TimeSpan DisconnectTimeout { get; set; }
    }
}
