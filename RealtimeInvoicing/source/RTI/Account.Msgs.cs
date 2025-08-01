namespace RTI {
    using global::ReactiveDomain.Messaging;

    public class AccountMsgs {
        public class Opened : Event {
            public readonly Guid Id;
            public readonly string Name;

            public Opened(Guid id, string name) {
                Id = id;
                Name = name;
            }
        }
    }
}