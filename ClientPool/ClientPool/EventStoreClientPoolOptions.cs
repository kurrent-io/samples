namespace ClientPool {
    using EventStore.Client;

    public class EventStoreClientPoolOptions {
        public int MaximumReaderThreads { get; set; } = 3;
        public int MaximumWriterThreads { get; set; } = 1;
        public Uri LeaderUri { get; set; }
        public Uri[] ReadNodeUris { get; set; }
        public UserCredentials DefaultCredentials { get; set; }
    }  
}
