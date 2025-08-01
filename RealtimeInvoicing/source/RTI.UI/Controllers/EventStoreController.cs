namespace RTI.UI.Controllers {
    using Microsoft.AspNetCore.Mvc;

    public class EventStoreController : Controller {
        private readonly InvoiceListRm _rm;

        public EventStoreController(InvoiceListRm rm) {
            _rm = rm ?? throw new ArgumentNullException(nameof(rm));
        }

        public IActionResult Index() {
            ViewBag.Checkpoints = new[] {new RTI.Models.Checkpoint { Id = "$ce-invoice", Position = _rm.Position ?? 0 } };
            return View("InvoiceList", _rm.Invoices);
        }

        public IActionResult Details(Guid id) {
            var model = _rm.Invoices.SingleOrDefault(inv=> inv.Id == id);
            return model == null ? NotFound() : View("InvoiceDetails", model);
        }
    }
}
