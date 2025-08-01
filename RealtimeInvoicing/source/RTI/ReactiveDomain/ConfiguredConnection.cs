namespace RTI.ReactiveDomain {
    using global::ReactiveDomain;
    using global::ReactiveDomain.Foundation;
    using global::ReactiveDomain.Messaging;
    using global::ReactiveDomain.Messaging.Bus;

    public class ConfiguredConnection : IConfiguredConnection {
        public ConfiguredConnection(
            IStreamStoreConnection conn,
            IStreamNameBuilder namer,
            IEventSerializer serializer) {
            Connection = conn;
            StreamNamer = namer;
            Serializer = serializer;
        }

        public IStreamStoreConnection Connection { get; }
        public IStreamNameBuilder StreamNamer { get; }

        public IEventSerializer Serializer { get; }

        public IListener GetListener(string name) => new StreamListener(name, Connection, StreamNamer, Serializer);

        public IListener GetQueuedListener(string name) => new QueuedStreamListener(name, Connection, StreamNamer, Serializer);

        public IStreamReader GetReader(string name, IHandle<IMessage> target = null) => new StreamReader(name, Connection, StreamNamer, Serializer, target);

        public IRepository GetRepository(bool caching = false) {
            IRepository repo = new StreamStoreRepository(StreamNamer, Connection, Serializer);
            return caching
                ? new ReadThroughAggregateCache(repo)
                : repo;
        }

        public ICorrelatedRepository GetCorrelatedRepository(
            IRepository baseRepository = null, bool caching = false) =>
            new CorrelatedStreamStoreRepository(baseRepository ?? GetRepository(caching));
    }
}