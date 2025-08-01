namespace RTI.KafkaConsumer.ToMongo {
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;
    using EventStore.StreamConnectors;
    using EventStore.StreamConnectors.Kafka;

    using Microsoft.Extensions.Logging;

    using MongoDB.Driver;

    internal class MongoDBConsumer : KafkaStreamListener {
        private IMongoCollection<Models.Invoice> _invoices;
        private IMongoCollection<Models.Checkpoint> _checkpoints;
        private JsonSerializerOptions _serializerOptions;
        private IMongoDatabase _mongo;

        public MongoDBConsumer(IMongoDatabase mongoDb, KafkaMongoConsumerConfigurationOptions options, IEventStoreConnection esConnection, ILoggerFactory loggerFactory) : base(options, esConnection, loggerFactory) {
            _mongo = mongoDb;

            _serializerOptions = new JsonSerializerOptions {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            _serializerOptions.Converters.Add(new EmptyGuidConverter());
        }

        protected override Task InitializeAsync() {
            _invoices = _mongo.GetCollection<Models.Invoice>(((KafkaMongoConsumerConfigurationOptions)Options).CollectionName);
            _checkpoints = _mongo.GetCollection<Models.Checkpoint>(Strings.Collections.Checkpoints);
            return Task.CompletedTask;
        }

        protected override async Task ProcessAsync(ProjectedEvent e) {
            var invoice = JsonSerializer.Deserialize<Models.Invoice>(e.Data, _serializerOptions);

            await _invoices.ReplaceOneAsync(
                filter: x => x.Id == invoice.Id,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: invoice
            );

            await UpdateCheckpointAsync(e.Position);
        }

        protected override Task CleanupAsync() => Task.CompletedTask;

        protected override async Task UpdateCheckpointAsync(long position) {
            await _checkpoints.ReplaceOneAsync(
                filter: x => x.Id == Options.Stream,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: new Models.Checkpoint { Id = Options.Stream, Position = position });
        }

        protected override Task<long?> ResolveLastCheckpointAsync() {
            var checkpoints = _checkpoints.Find(x => x.Id == Options.Stream);
            return Task.FromResult(checkpoints.Any()
                ? checkpoints.Single().Position
                : (long?)null);
        }
    }
}
