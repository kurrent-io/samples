namespace RTI.UI.Controllers {
    using System.Text.Json;

    using EventStore.StreamConnectors;

    using Microsoft.AspNetCore.Mvc;

    using StackExchange.Redis;

    public class RedisController : Controller {
        IDatabase _redis;
        ILogger _log;
        JsonSerializerOptions _serializerOptions;

        public RedisController(IDatabase redis, ILoggerFactory loggerFactory) {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _log = loggerFactory.CreateLogger<RedisController>();
            _serializerOptions = new JsonSerializerOptions {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            _serializerOptions.Converters.Add(new EmptyGuidConverter());
        }

        public IActionResult Index() {
            ViewBag.Checkpoints = _redis.HashGetAll(Strings.Collections.Checkpoints)
                .Select(he => new RTI.Models.Checkpoint { Id = he.Name, Position = Convert.ToInt64(he.Value) })
                .ToArray();
            var headers = _redis.HashGetAll(Strings.Collections.Invoice)
                .Select(he => JsonSerializer.Deserialize<RTI.Models.Invoice>(he.Value.ToString(), _serializerOptions))
                .OrderByDescending(header => header.Date);
            return View("InvoiceList", headers);
        }

        public async Task<IActionResult> Details(Guid id) {
            var json = await _redis.StringGetAsync($"{Strings.Collections.Invoice}-{id}");
            var model = JsonSerializer.Deserialize<RTI.Models.Invoice>(json, _serializerOptions);
            return View("InvoiceDetails", model);
        }
    }
}
