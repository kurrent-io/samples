namespace RTI {
    using global::ReactiveDomain.Messaging;

    public static class PaymentTermsMsgs {
        public class Created : Event {
            public readonly Guid Id;
            public readonly string Description;
            public readonly int NetDays;
            public readonly long InterestBps;

            public Created(Guid id, string description, int netDays, long interestBps) {
                Id = id;
                Description = description;
                NetDays = netDays;
                InterestBps = interestBps;
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

        public class NetDaysChanged : Event {
            public readonly Guid Id;
            public readonly int NetDays;

            public NetDaysChanged(Guid id, int netDays) {
                Id = id;
                NetDays = netDays;
            }
        }

        internal class InterestAmountChanged {
            public readonly Guid Id;
            public readonly long InterestBps;

            public InterestAmountChanged(Guid id, long interestBps) {
                Id = id;
                InterestBps = interestBps;
            }
        }
    }
}