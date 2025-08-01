namespace RTI.UI {
    using global::ReactiveDomain.Foundation;
    using global::ReactiveDomain.Messaging.Bus;

    using RTI.ReactiveDomain;

    using ReadModelBase = ReactiveDomain.ReadModelBase;

    /// <summary>
    /// NOTE: this is to allow a UI of the list of items that are within redis.
    /// </summary>
    public class InvoiceListRm : ReadModelBase,
        IHandle<InvoiceMsgs.Generated>,
        IHandle<InvoiceMsgs.PaymentTermsApplied>,
        IHandle<InvoiceMsgs.ItemAdded>,
        IHandle<InvoiceMsgs.ItemRemoved>,
        IHandle<InvoiceMsgs.Issued>,
        IHandle<InvoiceMsgs.ReIssued>,
        IHandle<InvoiceMsgs.PaymentApplied>,
        IHandle<InvoiceMsgs.PaymentVoided>,
        IHandle<InvoiceMsgs.StatusChanged>,
        IHandle<InvoiceMsgs.Closed> {
        private HashSet<RTI.Models.Invoice> _invoices = new();
        private IListener _listener;
        private long? _initialPos;

        internal IEnumerable<RTI.Models.Invoice> Invoices => _invoices.AsEnumerable();
        public long? Position => _listener?.Position > _initialPos
            ? _listener?.Position
            : _initialPos ?? 0;

        public InvoiceListRm(IConfiguredConnection conn) : base(nameof(InvoiceListRm), () => conn.GetQueuedListener(nameof(InvoiceListRm))) {

            long? pos = null;
            using (var reader = conn.GetReader(nameof(InvoiceListRm), this)) {
                WireSubscribers(reader.EventStream);
                reader.Read<Invoice>();
                pos = reader.Position;
            }

            _listener = conn.GetQueuedListener($"{nameof(InvoiceListRm)}-UI");
            WireSubscribers(_listener.EventStream);
            _listener.Start<Invoice>(_initialPos);
            _initialPos =  pos == null
                ? 0
                : pos == null
                    ? _listener.Position
                    : pos;
        }

        void WireSubscribers(ISubscriber eventStream) {
            eventStream.Subscribe<InvoiceMsgs.Generated>(this);
            eventStream.Subscribe<InvoiceMsgs.PaymentTermsApplied>(this);
            eventStream.Subscribe<InvoiceMsgs.ItemAdded>(this);
            eventStream.Subscribe<InvoiceMsgs.ItemRemoved>(this);
            eventStream.Subscribe<InvoiceMsgs.Issued>(this);
            eventStream.Subscribe<InvoiceMsgs.ReIssued>(this);
            eventStream.Subscribe<InvoiceMsgs.PaymentApplied>(this);
            eventStream.Subscribe<InvoiceMsgs.PaymentVoided>(this);
            eventStream.Subscribe<InvoiceMsgs.StatusChanged>(this);
            eventStream.Subscribe<InvoiceMsgs.Closed>(this);
        }

        public void Handle(InvoiceMsgs.Generated msg) {
            if (_invoices.Any(inv => inv.Id == msg.Id)) return; // assume duplicate.
            _invoices.Add(new RTI.Models.Invoice {
                AccountId = msg.AccountId,
                AccountName = msg.AccountName,
                BalanceDue = 0,
                Date = msg.Date,
                Id = msg.Id,
                Items = new(),
                Payments = new(),
                PaymentsTotal = 0,
                PaymentTermsId = Guid.Empty,
                Status = "Opened",
                Total = 0
            });
        }

        public void Handle(InvoiceMsgs.PaymentTermsApplied msg) {
            var invoice = _invoices.SingleOrDefault(i => i.Id == msg.Id);
            invoice.PaymentTermsId = msg.PaymentTermsId;
            invoice.PaymentTermsName = msg.Description;
        }

        public void Handle(InvoiceMsgs.ItemAdded msg) {
            var invoice = _invoices.SingleOrDefault(i => i.Id == msg.Id);
            invoice.Items.Add(new RTI.Models.InvoiceItem {
                Id = msg.LineItemId,
                Cost = msg.Cost,
                Description = msg.Description,
                ItemId = msg.ItemId,
                Quantity = msg.Quantity,
                SKU = msg.SKU,
                UnitPrice = msg.UnitPrice,
                Subtotal = msg.UnitPrice * msg.Quantity
            });

            RunCalculations(invoice);
        }

        public void Handle(InvoiceMsgs.ItemRemoved msg) {
            var invoice = _invoices.SingleOrDefault(i => i.Id == msg.Id);
            foreach (var rem in invoice.Items.Where(i => i.Id == msg.LineItemId).ToArray()) {
                invoice.Items.Remove(rem);
            }

            RunCalculations(invoice);
        }

        public void Handle(InvoiceMsgs.Issued msg) {
            var invoice = _invoices.SingleOrDefault(i => i.Id == msg.Id);
            invoice.Date = msg.Date;
            invoice.Status = "Issued";
        }

        public void Handle(InvoiceMsgs.ReIssued msg) {
            var invoice = _invoices.SingleOrDefault(i => i.Id == msg.Id);
            invoice.Date = msg.Date;
            invoice.Status = "Re-issued";
        }

        public void Handle(InvoiceMsgs.PaymentApplied msg) {
            var invoice = _invoices.SingleOrDefault(i => i.Id == msg.Id);
            invoice.Payments.Add(new RTI.Models.InvoicePayment {
                Amount = msg.Amount,
                Id = msg.PaymentId,
                Received = msg.Received
            });

            RunCalculations(invoice);
        }

        public void Handle(InvoiceMsgs.PaymentVoided msg) {
            var invoice = _invoices.SingleOrDefault(i => i.Id == msg.Id);
            foreach (var rem in invoice.Payments.Where(p => p.Id == msg.PaymentId).ToArray()) {
                invoice.Payments.Remove(rem);
            }

            RunCalculations(invoice);
        }

        public void Handle(InvoiceMsgs.StatusChanged msg) {
            var invoice = _invoices.SingleOrDefault(i => i.Id == msg.Id);
            invoice.Status = msg.Status.ToString();
        }

        public void Handle(InvoiceMsgs.Closed msg) {
            var invoice = _invoices.SingleOrDefault(i => i.Id == msg.Id);
            invoice.Status = "Closed";
        }

        private void RunCalculations(RTI.Models.Invoice invoice) {
            invoice.Total = 0;
            invoice.PaymentsTotal = 0;
            invoice.BalanceDue = 0;

            foreach (var i in invoice.Items) {
                invoice.Total += i.Subtotal;
            }

            foreach (var p in invoice.Payments) {
                invoice.PaymentsTotal += p.Amount;
            }

            invoice.BalanceDue = invoice.Total - invoice.PaymentsTotal;
        }
    }
}
