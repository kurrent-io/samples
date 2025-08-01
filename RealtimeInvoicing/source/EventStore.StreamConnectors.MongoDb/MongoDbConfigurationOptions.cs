namespace EventStore.StreamConnectors.MongoDb {
    public class MongoDbConfigurationOptions : StreamConfigurationOptions {
        public string CollectionName { get; set; }
    }
}
