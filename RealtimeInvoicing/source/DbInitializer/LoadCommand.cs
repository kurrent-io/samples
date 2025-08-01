namespace DbInitializer {
    using ReactiveDomain.Messaging;

    internal class LoadCommand : Command {
        public LoadCommand(CancellationToken? token = null) : base(token) {

        }
    }
}
