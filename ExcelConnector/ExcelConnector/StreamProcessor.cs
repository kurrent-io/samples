namespace ExcelConnector {
    using System;
    using System.Linq;

    using EventStore.ClientAPI;

    public abstract class StreamProcessor {
        private readonly IEventStoreConnection _connection;
        protected abstract string StreamName { get; }
        private readonly int _readPageSize = 500;

        public event EventHandler OnProcessed;

        public StreamProcessor(IEventStoreConnection connection) {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public void Read() {
            Initialize();

            var remaining = int.MaxValue;
            var sliceStart = ResolveLastCheckpoint();
            StreamEventsSlice currentSlice;
            bool isCompleted = false;

            do {
                var page = remaining < _readPageSize ? remaining : _readPageSize;
                currentSlice = _connection.ReadStreamEventsForwardAsync(
                    StreamName,
                    sliceStart,
                    page,
                    true).GetAwaiter().GetResult();


                if (currentSlice is not StreamEventsSlice) { // can this be null-checked instead?
                    isCompleted = true;
                } else {
                    remaining -= currentSlice.Events.Length;
                    sliceStart = currentSlice.NextEventNumber;
                    var events = currentSlice.Events.Where(e => e.Event.IsJson).ToArray();
                    foreach (var e in events) {
                        Process(e);
                        OnProcessed?.Invoke(this, EventArgs.Empty);
                    }
                }

                UpdateCheckpoint(currentSlice.NextEventNumber);
            } while ((!currentSlice.IsEndOfStream && remaining != 0) || isCompleted);

            Cleanup();
        }

        public void MonitorForChanges() {
            throw new NotImplementedException();
        }

        protected abstract void Initialize();
        protected abstract long ResolveLastCheckpoint();
        protected abstract void Process(ResolvedEvent e);
        protected abstract void UpdateCheckpoint(long position);
        protected abstract void Cleanup();
    }
}
