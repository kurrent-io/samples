namespace RTI.UI.Controllers {
    using Microsoft.AspNetCore.Mvc;

    using MongoDB.Driver;

    public class MongoDBController : Controller {
        IMongoDatabase _mongo;
        IMongoCollection<RTI.Models.Invoice> _invoices;
        IMongoCollection<RTI.Models.Checkpoint> _checkpoints;
        ILogger _log;

        public MongoDBController(IMongoDatabase mongo, ILoggerFactory loggerFactory) {
            _mongo = mongo ?? throw new ArgumentNullException(nameof(mongo));
            _invoices = _mongo.GetCollection<RTI.Models.Invoice>(Strings.Collections.Invoice);
            _checkpoints = _mongo.GetCollection<RTI.Models.Checkpoint>(Strings.Collections.Checkpoints);
            _log = loggerFactory.CreateLogger<MongoDBController>();
        }

        public async Task<IActionResult> Index() {
            ViewBag.Checkpoints = _checkpoints.AsQueryable().ToArray();
            return View("InvoiceList", await _invoices.AsQueryable().ToListAsync());
        }

        public async Task<IActionResult> Details(Guid id) {
            var invoice = await (await _invoices.FindAsync(filter: filter => filter.Id == id)).SingleOrDefaultAsync();

            return invoice != null
                ? View("InvoiceDetails", invoice)
                : NotFound();
        }
    }
}
