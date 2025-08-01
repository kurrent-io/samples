namespace ClientPool {
    using EventStore.Client;

    internal record ReadNodeInformation {
        public int ReadNodeIndex { get; }
        public Uri ServerUri { get; }
        public EventStoreClient Client { get; }

        public ReadNodeInformation(int readNodeIndex, Uri serverUri, EventStoreClient client) {
            ReadNodeIndex = readNodeIndex;
            ServerUri = serverUri;
            Client = client;
        }
    }
}