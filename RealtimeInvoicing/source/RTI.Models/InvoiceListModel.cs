namespace RTI.Models {
    public static class LongExtensions {
        public static decimal ToDollars(this long cents) => cents / 100M;
    }
    public class InvoiceHeader {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string AccountName { get; set; }
        public Guid PaymentTermsId { get; set; }
        public string PaymentTermsName { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public long Total { get; set; }
        public long PaymentsTotal { get; set; }
        public long BalanceDue { get; set; }
    }

    public class Invoice {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string AccountName { get; set; }
        public Guid PaymentTermsId { get; set; }
        public string PaymentTermsName { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public long Total { get; set; }
        public long PaymentsTotal { get; set; }
        public long BalanceDue { get; set; }
        public List<InvoiceItem> Items { get; set; } = new();
        public List<InvoicePayment> Payments { get; set; } = new();
    }

    public class InvoiceItem {
        public Guid Id { get; set; }
        public Guid ItemId { get; set; }
        public string SKU { get; set; }
        public string Description { get; set; }
        public long Cost { get; set; }
        public long UnitPrice { get; set; }
        public long Quantity { get; set; }
        public long Subtotal { get; set; }
        public bool HasBeenRemoved { get; set; }
    }

    public class InvoicePayment {
        public Guid Id { get; set; }
        public long Amount { get; set; }
        public DateTime Received { get; set; }
    }
}