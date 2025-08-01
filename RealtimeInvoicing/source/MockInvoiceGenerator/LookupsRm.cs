namespace MockInvoiceGenerator {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ReactiveDomain.Messaging;
    using ReactiveDomain.Messaging.Bus;

    using RTI;
    using RTI.ReactiveDomain;

    internal class LookupsRm : ReadModelBase, IHandle<ItemMsgs.Imported>, IHandle<AccountMsgs.Opened>, IHandle<PaymentTermsMsgs.Created> {
        private readonly HashSet<Guid> _itemIds = new();
        private readonly HashSet<Guid> _accountIds = new();
        private readonly HashSet<Guid> _paymentTermsIds = new();

        private readonly ICorrelatedRepository _repository;

        private readonly Random _rand = new();

        public LookupsRm(IConfiguredConnection conn) : base(nameof(LookupsRm), () => conn.GetQueuedListener(nameof(LookupsRm))) {
            _repository = conn.GetCorrelatedRepository();

            EventStream.Subscribe<ItemMsgs.Imported>(this);
            EventStream.Subscribe<AccountMsgs.Opened>(this);
            EventStream.Subscribe<PaymentTermsMsgs.Created>(this);

            long? itemPos;
            long? accountPos;
            long? paymentTermsPos;

            using (var reader = conn.GetReader(nameof(LookupsRm), this)) {
                reader.Read<Item>();
                itemPos = reader.Position;

                reader.Read<Account>();
                accountPos = reader.Position;

                reader.Read<PaymentTerms>();
                paymentTermsPos = reader.Position;
            }

            Start<Item>(itemPos);
            Start<Account>(accountPos);
            Start<PaymentTerms>(paymentTermsPos);
        }

        public Item GetRandomItem(ICorrelatedMessage msg) {
            if (_itemIds.Count == 0) return null;

            var idx = _rand.Next(0, _itemIds.Count - 1);
            var id = _itemIds.Skip(idx).FirstOrDefault();
            if (id == Guid.Empty) id = _itemIds.Last();
            return _repository.GetById<Item>(id, msg);
        }

        public Account GetRandomAccount(ICorrelatedMessage msg) {
            if(_accountIds.Count == 0) return null;

            var idx = _rand.Next(0, _accountIds.Count - 1);
            var id = _accountIds.Skip(idx).FirstOrDefault();
            if (id == Guid.Empty) id = _accountIds.Last();
            return _repository.GetById<Account>(id, msg);
        }

        public PaymentTerms GetRandomPaymentTerms(ICorrelatedMessage msg) {
            if(_paymentTermsIds.Count == 0) return null;

            var idx = _rand.Next(0, _paymentTermsIds.Count - 1);
            var id = _paymentTermsIds.Skip(idx).FirstOrDefault();
            if (id == Guid.Empty) id = _itemIds.Last();
            return _repository.GetById<PaymentTerms>(id, msg);
        }

        public void Handle(ItemMsgs.Imported message) => _itemIds.Add(message.Id);

        public void Handle(AccountMsgs.Opened message) => _accountIds.Add(message.Id);

        public void Handle(PaymentTermsMsgs.Created message) => _paymentTermsIds.Add(message.Id);
    }
}
