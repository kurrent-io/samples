namespace RTI {
    using System;

    using global::ReactiveDomain.Messaging;
    using global::ReactiveDomain.Util;

    using RTI.ReactiveDomain;

    public class PaymentTerms : AggregateRoot {
        internal string Description { get; private set; }
        internal int NetDays { get; private set; }
        internal long InterestBps { get; private set; }

        public PaymentTerms(Guid id, string description, int netDays, long interestBps, ICorrelatedMessage msg) : base(msg) {
            Ensure.NotEmptyGuid(id, nameof(id));
            Ensure.NotNullOrEmpty(description, nameof(description));
            Ensure.GreaterThanOrEqualTo(0, netDays, nameof(netDays));
            Ensure.GreaterThanOrEqualTo(0, interestBps, nameof(interestBps));

            RegisterHandlers();

            Raise(new PaymentTermsMsgs.Created(id, description, netDays, interestBps));
        }

        public PaymentTerms() : base(null) {
            RegisterHandlers();
        }

        private void RegisterHandlers() {
            Register<PaymentTermsMsgs.Created>(x => {
                Id = x.Id;
                Description = x.Description;
                NetDays = x.NetDays;
                InterestBps = x.InterestBps;
            });
            Register<PaymentTermsMsgs.DescriptionChanged>(x => Description = x.Description);
            Register<PaymentTermsMsgs.NetDaysChanged>(x => NetDays = x.NetDays);
            Register<PaymentTermsMsgs.InterestAmountChanged>(x => InterestBps = x.InterestBps);
        }

        public void ChangeDescription(string description) {
            Ensure.NotNullOrEmpty(description, nameof(description));
            if (description.Equals(Description)) return;
            Raise(new PaymentTermsMsgs.DescriptionChanged(Id, description));
        }

        public void ChangeNetDays(int netDays) {
            Ensure.GreaterThanOrEqualTo(0, netDays, nameof(netDays));
            if (netDays == NetDays) return;
            Raise(new PaymentTermsMsgs.NetDaysChanged(Id, netDays));
        }

        public void ChangeInterestAmount(long interestBps) {
            Ensure.GreaterThanOrEqualTo(NetDays, interestBps, nameof(interestBps));
            if (interestBps == InterestBps) return;
            Raise(new PaymentTermsMsgs.InterestAmountChanged(Id, interestBps));
        }
    }
}