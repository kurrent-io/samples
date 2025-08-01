namespace RTI {
    using global::ReactiveDomain.Messaging;

    public static class InvoiceMsgs {
        public class Generated : Event {
            public readonly Guid Id;
            public readonly Guid AccountId;
            public readonly string AccountName;
            public readonly DateTime Date;

            public Generated(Guid id, Guid accountId, string accountName, DateTime date) {
                Id = id;
                AccountId = accountId;
                AccountName = accountName;
                Date = date;
            }
        }

        public class PaymentTermsApplied : Event {
            public readonly Guid Id;
            public readonly Guid PaymentTermsId;
            public readonly string Description;
            public readonly long InterestBps;

            public PaymentTermsApplied(Guid id, Guid paymentTermsId, string description, long interestBps) {
                Id = id;
                PaymentTermsId = paymentTermsId;
                Description = description;
                InterestBps = interestBps;
            }
        }

        public class ItemAdded : Event {
            public readonly Guid Id;
            public readonly Guid LineItemId;
            public readonly Guid ItemId;
            public readonly string SKU;
            public readonly string Description;
            public readonly long Cost;
            public readonly long UnitPrice;
            public readonly int Quantity; // future: how do we express between per-pound, per-gram, per-ounce, StockNo, etc.
            public readonly long Balance;

            public ItemAdded(Guid id, Guid lineItemId, Guid itemId, string sku, string description, long cost, long unitPrice, int quantity, long balance) {
                Id = id;
                LineItemId = lineItemId;
                ItemId = itemId;
                SKU = sku;
                Description = description;
                Cost = cost;
                UnitPrice = unitPrice;
                Quantity = quantity;
                Balance = balance;
            }
        }

        public class ItemRemoved : Event {
            public readonly Guid Id;
            public readonly Guid LineItemId;
            public readonly long Balance;

            public ItemRemoved(Guid id, Guid lineItemId, long balance) {
                Id = id;
                LineItemId = lineItemId;
                Balance = balance;
            }
        }

        public class Issued : Event {
            public readonly Guid Id;
            public readonly DateTime Date;

            public Issued(Guid id, DateTime date) {
                Id = id;
                Date = date;
            }
        }

        public class ReIssued : Event {
            public readonly Guid Id;
            public readonly DateTime Date;

            public ReIssued(Guid id, DateTime date) {
                Id = id;
                Date = date;
            }
        }

        public class PaymentApplied : Event {
            public readonly Guid Id;
            public readonly Guid PaymentId;
            public readonly long Amount;
            public readonly long Balance;
            public readonly DateTime Received;

            public PaymentApplied(Guid id, Guid paymentId, long amount, long balance, DateTime received) {
                Id = id;
                PaymentId = paymentId;
                Amount = amount;
                Balance = balance;
                Received = received;
            }
        }

        public class PaymentVoided : Event {
            public readonly Guid Id;
            public readonly Guid PaymentId;
            public readonly long Balance;
            public readonly DateTime Voided;
            public readonly DateTime Cleared;

            public PaymentVoided(Guid id, Guid paymentId, long balance, DateTime voided, DateTime cleared) {
                Id = id;
                PaymentId = paymentId;
                Balance = balance;
                Voided = voided;
                Cleared = cleared;
            }
        }

        public class StatusChanged : Event {
            public readonly Guid Id;
            public readonly Status Status;

            public StatusChanged(Guid id, Status status) {
                Id = id;
                Status = status;
            }
        }

        public class Closed : Event {
            public readonly Guid Id;
            public readonly DateTime Date;

            public Closed(Guid id, DateTime date) {
                Id = id;
                Date = date;
            }
        }

        public enum Status {
            NotSet,
            Open,
            Invoiced,
            PastDue,
            Closed
        }
    }
}