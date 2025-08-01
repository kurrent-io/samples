namespace EventStore.StreamConnectors.MongoDb {
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;

    using Microsoft.Extensions.Logging;

    using MongoDB.Driver;

    public class MongoDbStreamProcessor<TDocument> : MongoDbStreamProcessor where TDocument : IMongoDocument, new() {
        private IMongoCollection<TDocument> _collection;

        public MongoDbStreamProcessor(MongoDbConfigurationOptions options, IMongoDatabase mongoDb, IEventStoreConnection connection, ILoggerFactory loggerFactory) 
            : base(options, mongoDb, connection, loggerFactory) {
        }

        protected override async Task InitializeAsync(CancellationToken token) {
            await base.InitializeAsync(token);
            _collection = MongoDb.GetCollection<TDocument>(Options.CollectionName.Replace("$", ""));
        }

        protected override async Task ProcessAsync(ResolvedEvent e) {
            var doc = (await _collection.FindAsync(doc => doc.Id == e.Event.EventStreamId)).SingleOrDefault()
                ?? new TDocument { Id = e.Event.EventStreamId };
            var docType = doc.GetType();
            var json = JsonDocument.Parse(e.Event.Data);

            foreach (var p in json.RootElement.EnumerateObject()) {
                if (p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;

                var docProp = docType.GetProperty(p.Name);
                if (docProp == null) continue;

                docProp.SetValue(doc, Convert.ChangeType(p.Value.ToString(), docProp.PropertyType), null);
            }

            await _collection.ReplaceOneAsync(
                filter: x => x.Id == e.Event.EventStreamId,
                options: new ReplaceOptions { IsUpsert = true },
                replacement: doc);
        }
    }
}
