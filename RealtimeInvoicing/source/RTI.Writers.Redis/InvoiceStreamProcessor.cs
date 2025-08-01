namespace RTI.Writers.Redis {
    using System.Text.Json;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;
    using EventStore.StreamConnectors;
    using EventStore.StreamConnectors.Redis;

    using Microsoft.Extensions.Logging;

    using StackExchange.Redis;

    internal class InvoiceStreamProcessor : RedisStreamProcessor {
        JsonSerializerOptions _serializerOptions;
        public InvoiceStreamProcessor(IDatabase redis, RedisConfigurationOptions options, IEventStoreConnection connection, ILoggerFactory loggerFactory) : base(redis, options, connection, loggerFactory) {
            _serializerOptions = new JsonSerializerOptions {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            _serializerOptions.Converters.Add(new EmptyGuidConverter());
        }

        protected override async Task ProcessAsync(ResolvedEvent e) {
            try {
                var invoice = JsonSerializer.Deserialize<Models.Invoice>(e.Event.Data, _serializerOptions);
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

                var key = $"{Options.KeyPrefix}-{invoice.Id}";
                await Redis.StringSetAsync(key, JsonSerializer.Serialize(invoice, _serializerOptions));
                await Redis.HashSetAsync(Options.KeyPrefix, new[] { new HashEntry(header.Id.ToString().ToLower(), JsonSerializer.Serialize(header, _serializerOptions)) });
            } catch (Exception ex) {
                Log.LogCritical(ex, "Something happened.");
                throw;
            }
        }
    }
}
