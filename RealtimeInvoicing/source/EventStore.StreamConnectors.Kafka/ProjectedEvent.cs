namespace EventStore.StreamConnectors.Kafka {
    public record ProjectedEvent {
        public string EventStreamId { get; set; }
        public string EventType { get; set; }
        public byte[] Metadata { get; set; }
        public long Position { get; set; }
        public string Data { get; set; }
    }
}
