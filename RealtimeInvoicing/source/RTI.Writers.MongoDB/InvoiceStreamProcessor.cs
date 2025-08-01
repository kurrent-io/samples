namespace RTI.Writers.MongoDB {
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;
    using EventStore.StreamConnectors;
    using EventStore.StreamConnectors.MongoDb;

    using global::MongoDB.Driver;

    using Microsoft.Extensions.Logging;

    internal class InvoiceStreamProcessor : MongoDbStreamProcessor {
        IMongoCollection<Models.Invoice> _invoices;
        JsonSerializerOptions _serializerOptions;

        public InvoiceStreamProcessor(MongoDbConfigurationOptions options, IMongoDatabase mongoDb, IEventStoreConnection connection, ILoggerFactory loggerFactory)
            : base(options, mongoDb, connection, loggerFactory) {
            _serializerOptions = new JsonSerializerOptions {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            _serializerOptions.Converters.Add(new EmptyGuidConverter());
        }

        protected override async Task InitializeAsync(CancellationToken token) {
            await base.InitializeAsync(token);
            _invoices = MongoDb.GetCollection<Models.Invoice>(Options.CollectionName);
        }

        protected override async Task ProcessAsync(ResolvedEvent e) {
            var invoice = JsonSerializer.Deserialize<Models.Invoice>(e.Event.Data, _serializerOptions);

            await _invoices.ReplaceOneAsync(
                filter: x => x.Id == invoice.Id,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: invoice);
        }
    }
}
