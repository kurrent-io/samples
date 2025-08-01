namespace RTI {
    using System;

    using global::ReactiveDomain.Messaging;
    using global::ReactiveDomain.Util;

    using RTI.ReactiveDomain;

    public class Account : AggregateRoot {
        internal string Name { get; private set; }

        public Account(Guid id, string accountName, ICorrelatedMessage msg) : base(msg) {
            Ensure.NotEmptyGuid(id, nameof(id));
            Ensure.NotNullOrEmpty(accountName, nameof(accountName));

            RegisterHandlers();

            Raise(new AccountMsgs.Opened(id, accountName));
        }

        public Account() : base(null) {
            RegisterHandlers();
        }

        private void RegisterHandlers() {
            Register<AccountMsgs.Opened>(o => {
                Id = o.Id;
                Name = o.Name;
            });
        }
    }
}