namespace RTI.UI {
    using System.Text.Json;

    using EventStore.ClientAPI;
    using EventStore.StreamConnectors;

    public class CurrentBackplaneVm : IDisposable {
        private readonly IEventStoreConnection _connection;
        private EventStoreStreamCatchUpSubscription _subscription;

        public Backplanes CurrentBackplane { get; set; } = Backplanes.NotSet;

        public CurrentBackplaneVm(IEventStoreConnection connection) {
            _connection = connection;
        }

        public async Task StartAsync() {
            var slice = await _connection.ReadStreamEventsBackwardAsync(
                KnownStreams.StreamProcessor,
                StreamPosition.End,
                1,
                true);
            if (slice is not StreamEventsSlice) { // can this be null-checked instead?
                CurrentBackplane = Backplanes.NotSet;
                return;
            } else if (slice.Events.Length == 0) {
                await ChangeBackplaneAsync(Backplanes.Direct);
            } else {
                var signal = JsonSerializer.Deserialize<StreamProcessorSignal>(slice.Events[0].Event.Data);
                CurrentBackplane = signal?.ActiveBackplane ?? Backplanes.NotSet;
            }

            _subscription = _connection.SubscribeToStreamFrom(
                KnownStreams.StreamProcessor,
                slice.Events.Length == 0 ? null : slice.Events[0].Event.EventNumber,
                CatchUpSubscriptionSettings.Default,
                (sub, e) => {
                    var signal = JsonSerializer.Deserialize<StreamProcessorSignal>(e.Event.Data);
                    CurrentBackplane = signal?.ActiveBackplane ?? Backplanes.NotSet;
                });
        }

        public async Task ChangeBackplaneAsync(Backplanes backplane) {
            if (backplane == Backplanes.NotSet) throw new ArgumentException(nameof(backplane));
            await _connection.AppendToStreamAsync(KnownStreams.StreamProcessor, ExpectedVersion.Any, new[] {
                new EventData(Guid.NewGuid(), nameof(StreamProcessorSignal), true, JsonSerializer.SerializeToUtf8Bytes(new StreamProcessorSignal{ ActiveBackplane = backplane }), Array.Empty<byte>())
            });
        }

        public void Dispose() {
            _subscription?.Stop();
            _subscription = null;
        }
    }
}
