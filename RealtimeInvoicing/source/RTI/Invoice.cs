namespace RTI {
    using global::ReactiveDomain.Messaging;
    using global::ReactiveDomain.Util;

    using RTI.ReactiveDomain;

    public class Invoice : AggregateRoot {
        private InvoiceMsgs.Status _currentStatus;
        private Guid _paymentTermsId = Guid.Empty;
        private readonly HashSet<LineItem> _lineItems = new();
        private readonly HashSet<PaymentLE> _appliedPayments = new();
        public long Balance { get; private set; }

        public Invoice(Guid id, Account account, DateTime date, ICorrelatedMessage msg) : base(msg) {
            Ensure.NotEmptyGuid(id, nameof(id));
            Ensure.NotNull(account, nameof(account));
            Ensure.NotDefault(date, nameof(date));

            RegisterHandlers();

            Raise(new InvoiceMsgs.Generated(id, account.Id, account.Name, date));
            Raise(new InvoiceMsgs.StatusChanged(id, InvoiceMsgs.Status.Open));
        }

        public Invoice() : base(null) {
            RegisterHandlers();
        }

        private void RegisterHandlers() {
            Register<InvoiceMsgs.Generated>(x => Id = x.Id);
            Register<InvoiceMsgs.ItemAdded>(item => {
                _lineItems.Add(new LineItem {
                    Id = item.LineItemId,
                    Cost = item.Cost,
                    Description = item.Description,
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    SKU = item.SKU,
                    UnitPrice = item.UnitPrice
                });
                Balance = item.Balance;
            });
            Register<InvoiceMsgs.ItemRemoved>(item => {
                _lineItems.RemoveWhere(li => li.Id == item.LineItemId); 
                Balance = item.Balance;
            });
            Register<InvoiceMsgs.PaymentApplied>(pmt => {
                _appliedPayments.Add(new PaymentLE { Id = pmt.PaymentId, Amount = pmt.Amount, Received = pmt.Received });
                Balance = pmt.Balance;
            });
            Register<InvoiceMsgs.PaymentVoided>(pmt => {
                _appliedPayments.RemoveWhere(le => le.Id == pmt.PaymentId);
                Balance = pmt.Balance;
            });
            Register<InvoiceMsgs.StatusChanged>(x => _currentStatus = x.Status);
            Register<InvoiceMsgs.Issued>(_ => _currentStatus = InvoiceMsgs.Status.Invoiced);
            Register<InvoiceMsgs.Closed>(c => {
                _currentStatus = InvoiceMsgs.Status.Closed;
            });
        }

        public void Set(PaymentTerms terms) {
            Ensure.NotNull(terms, nameof(terms));
            if (_paymentTermsId == terms.Id) return;
            Raise(new InvoiceMsgs.PaymentTermsApplied(Id, terms.Id, terms.Description, terms.InterestBps));
        }

        public void Add(Guid lineItemId, Item item, int quantity = 1) {
            Ensure.NotEmptyGuid(lineItemId, nameof(lineItemId));
            Ensure.NotNull(item, nameof(item));

            var itemsTotal = _lineItems.Where(li => li.Id != lineItemId).Sum(li => li.UnitPrice * li.Quantity) + (item.UnitPrice * quantity);
            var paymentsTotal = _appliedPayments.Sum(pmt => pmt.Amount);
            var balanceDue = itemsTotal - paymentsTotal;

            Raise(new InvoiceMsgs.ItemAdded(Id, lineItemId, item.Id, item.SKU, item.Description, item.Cost, item.UnitPrice, quantity, balanceDue));
        }

        public void Remove(Guid lineItemId) {
            var removals = _lineItems.Where(li => li.Id == lineItemId).ToArray();
            foreach (var removal in removals) {
                var itemsTotal = _lineItems.Where(li => li.Id != lineItemId).Sum(li => li.UnitPrice * li.Quantity);
                var paymentsTotal = _appliedPayments.Sum(pmt => pmt.Amount);
                var balanceDue = itemsTotal - paymentsTotal;
                Raise(new InvoiceMsgs.ItemRemoved(Id, removal.Id, balanceDue));
            }
        }

        public void ApplyPayment(Guid paymentId, long amount, DateTime received) {
            Ensure.NotEmptyGuid(paymentId, nameof(paymentId));
            Ensure.GreaterThanOrEqualTo(0, amount, nameof(amount));
            Ensure.NotDefault(received, nameof(received));

            var itemsTotal = _lineItems.Sum(li => li.UnitPrice * li.Quantity);
            var paymentsTotal = _appliedPayments.Sum(pmt => pmt.Amount);
            var balanceDue = itemsTotal - paymentsTotal;
            if (amount > balanceDue) throw new Exception("Payments may not be greater than the balance due.");
            Raise(new InvoiceMsgs.PaymentApplied(Id, paymentId, amount, balanceDue - amount, received));
        }

        public void VoidPayment(Guid paymentId, DateTime date, DateTime cleared) {
            var removals = _appliedPayments.Where(pmt => pmt.Id == paymentId).ToArray();
            foreach (var removal in removals) {
                var itemsTotal = _lineItems.Sum(li => li.UnitPrice * li.Quantity);
                var paymentsTotal = _appliedPayments.Where(pmt => pmt.Id != paymentId).Sum(pmt => pmt.Amount);
                var rmvPayment = _appliedPayments.Where(pmt => pmt.Id == paymentId).FirstOrDefault();
                var balanceDue = itemsTotal - paymentsTotal;

                Raise(new InvoiceMsgs.PaymentVoided(Id, paymentId, balanceDue = rmvPayment?.Amount ?? 0, date, cleared));
            }
        }

        public void ChangeInvoiceStatus(InvoiceMsgs.Status status) {
            if (_currentStatus.Equals(status)) return;
            Raise(new InvoiceMsgs.StatusChanged(Id, status));
        }

        public void Close(DateTime time) {
            if (_currentStatus == InvoiceMsgs.Status.Closed) return;
            if (_currentStatus == InvoiceMsgs.Status.Open) throw new Exception("Invoice must be issued before it can be closed.");


            //var balance = _lineItems.Sum(x => x.UnitPrice * x.Cost)
            //    -
            //    _appliedPayments.Sum(x => x.Amount);

            //if (balance != 0) throw new Exception("All outstanding payments must be applied before closing the invoice.");

            Raise(new InvoiceMsgs.Closed(Id, time));
        }

        private record PaymentLE {
            public Guid Id { get; init; }
            public long Amount { get; init; }
            public DateTime Received { get; init; }
        }

        private record LineItem {
            public Guid Id { get; init; }
            public Guid ItemId { get; init; }
            public string SKU { get; init; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public long Cost { get; set; }
            public long UnitPrice { get; set; }
            public int Quantity { get; set; }
        }
    }
}