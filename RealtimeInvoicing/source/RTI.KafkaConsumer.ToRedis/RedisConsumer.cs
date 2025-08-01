namespace RTI.KafkaConsumers.Redis {
    using System.Text.Json;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;
    using EventStore.StreamConnectors;
    using EventStore.StreamConnectors.Kafka;

    using Microsoft.Extensions.Logging;

    using StackExchange.Redis;

    internal class RedisConsumer : KafkaStreamListener {
        JsonSerializerOptions _serializerOptions;
        protected IDatabase Redis { get; private set; }

        public RedisConsumer(IDatabase redis, KafkaRedisConsumerConfigurationOptions options, IEventStoreConnection esConnection, ILoggerFactory loggerFactory) : base(options, esConnection, loggerFactory) {
            Redis = redis;
            _serializerOptions = new JsonSerializerOptions {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            _serializerOptions.Converters.Add(new EmptyGuidConverter());
        }

        protected override Task InitializeAsync() => Task.CompletedTask;

        protected override async Task<long?> ResolveLastCheckpointAsync() => await Redis.HashExistsAsync(Strings.Collections.Checkpoints, Options.Stream)
            ? (long?)(await Redis.HashGetAsync(Strings.Collections.Checkpoints, Options.Stream))
            : null;

        protected override async Task ProcessAsync(ProjectedEvent e) {
            var invoice = JsonSerializer.Deserialize<RTI.Models.Invoice>(e.Data, _serializerOptions);
            var header = new RTI.Models.InvoiceHeader {
                AccountId = invoice.AccountId,
                AccountName = invoice.AccountName,
                BalanceDue = invoice.BalanceDue,
                Date = invoice.Date,
                Id = invoice.Id,
                PaymentsTotal = invoice.PaymentsTotal,
                PaymentTermsId = invoice.PaymentTermsId,
                PaymentTermsName = invoice.PaymentTermsName,
                Status = invoice.Status,
                Total = invoice.Total,
            };

            var key = $"{((KafkaRedisConsumerConfigurationOptions)Options).KeyPrefix}-{invoice.Id}";
            await Redis.StringSetAsync(key, JsonSerializer.Serialize(invoice, _serializerOptions));
            await Redis.HashSetAsync(((KafkaRedisConsumerConfigurationOptions)Options).KeyPrefix, new[] { new HashEntry(header.Id.ToString().ToLower(), JsonSerializer.Serialize(header, _serializerOptions)) });


            await UpdateCheckpointAsync(e.Position);
        }

        protected override Task CleanupAsync() => Task.CompletedTask;

        protected override async Task UpdateCheckpointAsync(long position) => await Redis.HashSetAsync(Strings.Collections.Checkpoints, new[] { new HashEntry(Options.Stream, position) });
    }
}
