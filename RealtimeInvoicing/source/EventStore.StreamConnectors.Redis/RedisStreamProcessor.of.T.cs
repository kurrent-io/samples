namespace EventStore.StreamConnectors.Redis {
    using System.Text.Json;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;

    using Microsoft.Extensions.Logging;

    using StackExchange.Redis;

    public class RedisStreamProcessor<TDocument> : RedisStreamProcessor where TDocument : new() {
        public RedisStreamProcessor(IDatabase redis, RedisConfigurationOptions options, IEventStoreConnection connection, ILoggerFactory loggerFactory) : base(redis, options, connection, loggerFactory) {
        }

        protected override async Task ProcessAsync(ResolvedEvent e) {
            var streamIdParts = e.Event.EventStreamId.Split("-");
            var category = streamIdParts[0];
            var streamId = streamIdParts.Length > 1
                ? streamIdParts[1]
                : string.Empty;
            var keySuffix = string.IsNullOrWhiteSpace(streamId)
                ? category
                : streamId;
            var key = $"{Options.KeyPrefix}-{keySuffix}";

            var docBody = await Redis.StringGetAsync(key);
            var doc = docBody.HasValue
                ? JsonSerializer.Deserialize<TDocument>(docBody.ToString())
                : new TDocument();
            var docType = typeof(TDocument);
            var json = JsonDocument.Parse(e.Event.Data);

            foreach (var p in json.RootElement.EnumerateObject()) {
                if (p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;

                var docProp = docType.GetProperty(p.Name);
                if (docProp == null) continue;

                docProp.SetValue(doc, Convert.ChangeType(p.Value.ToString(), docProp.PropertyType), null);
            }

            await Redis.StringSetAsync(key, JsonSerializer.Serialize(doc));
        }
    }
}
