namespace EventStore.StreamConnectors.MongoDb {
    internal class Checkpoint {
        public string Id { get; set; }
        public long Position { get; set; }
    }
}
