namespace EventStore.StreamConnectors.MongoDb {
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;

    using Microsoft.Extensions.Logging;

    using MongoDB.Driver;

    public abstract class MongoDbStreamProcessor : StreamProcessor {
        protected MongoDbConfigurationOptions Options { get; private set; }
        protected IMongoDatabase MongoDb { get; private set; }

        private IMongoCollection<Checkpoint> _checkpoints;

        public MongoDbStreamProcessor(MongoDbConfigurationOptions options, IMongoDatabase mongoDb, IEventStoreConnection connection, ILoggerFactory loggerFactory) : base(options, connection, loggerFactory) {
            Monitor = new StreamProcessorActivationMonitor(this, connection, loggerFactory, Backplanes.Direct);
            Options = options;
            MongoDb = mongoDb;
        }

        protected override Task InitializeAsync(CancellationToken token) {
            _checkpoints = MongoDb.GetCollection<Checkpoint>("checkpoints");
            return Task.CompletedTask;
        }

        protected override Task CleanupAsync() => Task.CompletedTask;

        protected override Task<long?> ResolveLastCheckpointAsync() {
            var checkpoints = _checkpoints.Find(x => x.Id == Options.Stream);
            return Task.FromResult(checkpoints.Any()
                ? checkpoints.Single().Position
                : (long?)null);
        }

        protected override async Task UpdateCheckpointAsync(long position) {
            var checkpoints = _checkpoints.Find(x => x.Id == Options.Stream);
            if (checkpoints.Any()) {
                var checkpoint = checkpoints.Single();
                checkpoint.Position = position;
                await _checkpoints.ReplaceOneAsync(x => x.Id == Options.Stream, checkpoint);
            } else {
                await _checkpoints.InsertOneAsync(new Checkpoint { Id = Options.Stream, Position = position });
            }
        }
    }
}
