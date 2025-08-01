namespace RTI {
    using System;

    using global::ReactiveDomain.Messaging;
    using global::ReactiveDomain.Util;

    using RTI.ReactiveDomain;

    public class Item : AggregateRoot {
        internal string SKU { get;private set; }
        internal string Description { get;private set; }
        internal long Cost { get;private set; }
        internal long UnitPrice { get;private set; }

        public Item(Guid id, string sku, string description, long cost, long unitPrice, ICorrelatedMessage msg) : base(msg) {
            Ensure.NotEmptyGuid(id, nameof(id));
            Ensure.NotNullOrEmpty(sku, nameof(sku));
            Ensure.NotNullOrEmpty(description, nameof(description));
            Ensure.GreaterThanOrEqualTo(0, cost, nameof(cost));
            Ensure.GreaterThanOrEqualTo(0, unitPrice, nameof(unitPrice));

            RegisterHandlers();

            Raise(new ItemMsgs.Imported(id, sku, description, cost, unitPrice));
        }

        public Item() : base(null) {
            RegisterHandlers();
        }

        private void RegisterHandlers() {
            Register<ItemMsgs.Imported>(x => {
                Id = x.Id;
                SKU = x.SKU;
                Description = x.Description;
                Cost = x.Cost;
                UnitPrice = x.UnitPrice;
            });
            Register<ItemMsgs.DescriptionChanged>(x => Description = x.Description);
            Register<ItemMsgs.CostChanged>(x => Cost = x.Cost);
            Register<ItemMsgs.UnitPriceChanged>(x => UnitPrice = x.UnitPrice);
        }

        public void ChangeDescription(string description) {
            Ensure.NotNullOrEmpty(description, nameof(description));
            if (description.Equals(Description)) return;
            Raise(new ItemMsgs.DescriptionChanged(Id, description));
        }

        public void ChangeCost(long cost) {
            Ensure.GreaterThanOrEqualTo(0, cost, nameof(cost));
            if(cost.Equals(Cost)) return;
            Raise(new ItemMsgs.CostChanged(Id, cost));
        }

        public void ChangeUnitPrice(long unitPrice) { 
            Ensure.GreaterThanOrEqualTo(0, unitPrice, nameof(unitPrice));
            if(unitPrice.Equals(UnitPrice)) return;
            Raise(new ItemMsgs.UnitPriceChanged(Id, unitPrice));
        }
    }
}