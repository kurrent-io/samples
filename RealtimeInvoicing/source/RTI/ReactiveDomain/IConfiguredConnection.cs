namespace RTI.ReactiveDomain {
    using global::ReactiveDomain;
    using global::ReactiveDomain.Foundation;
    using global::ReactiveDomain.Messaging;
    using global::ReactiveDomain.Messaging.Bus;

    public interface IConfiguredConnection {
        IStreamStoreConnection Connection { get; }
        IStreamNameBuilder StreamNamer { get; }
        IEventSerializer Serializer { get; }
        IListener GetListener(string name);
        IListener GetQueuedListener(string name);
        IStreamReader GetReader(string name, IHandle<IMessage> target = null);
        IRepository GetRepository(bool caching = false);
        ICorrelatedRepository GetCorrelatedRepository(IRepository baseRepository = null, bool caching = false);
    }
}