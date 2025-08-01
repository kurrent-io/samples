namespace MockInvoiceGenerator {
    using ReactiveDomain.Messaging;

    internal class DefaultCommand : Command {
        public DefaultCommand(CancellationToken? token = null) : base(token) {

        }
    }
}
