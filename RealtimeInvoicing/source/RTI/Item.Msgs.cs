namespace RTI {
    using global::ReactiveDomain.Messaging;

    public static class ItemMsgs {
        public class Imported : Event {
            public readonly Guid Id;
            public readonly string SKU;
            public readonly string Description;
            public readonly long Cost;
            public readonly long UnitPrice;

            public Imported(Guid id, string sku, string description, long cost, long unitPrice) {
                Id = id;
                SKU = sku;
                Description = description;
                Cost = cost;
                UnitPrice = unitPrice;
            }
        }

        public class DescriptionChanged : Event {
            public readonly Guid Id;
            public readonly string Description;

            public DescriptionChanged(Guid id, string description) {
                Id = id;
                Description = description;
            }
        }

        public class CostChanged : Event {
            public readonly Guid Id;
            public readonly long Cost;

            public CostChanged(Guid id, long cost) {
                Id = id;
                Cost = cost;
            }
        }

        public class UnitPriceChanged : Event {
            public readonly Guid Id;
            public readonly long UnitPrice;

            public UnitPriceChanged(Guid id, long unitPrice) {
                Id = id;
                UnitPrice = unitPrice;
            }
        }
    }
}