namespace EventStore.StreamConnectors {
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public abstract class StreamProcessor : BackgroundService, IProcessorControl {
        private readonly StreamConfigurationOptions _options;
        private readonly IEventStoreConnection _connection;
        private CancellationToken _appToken;
        protected ILogger Log { get; }
        private readonly int _readPageSize = 500;
        private bool _disposed;
        private EventStoreCatchUpSubscription _subscription;
        protected StreamProcessorActivationMonitor Monitor { get; set; }

        public event EventHandler OnProcessed;

        public StreamProcessorStates State { get; private set; }

        public StreamProcessor(StreamConfigurationOptions options, IEventStoreConnection connection, ILoggerFactory logger) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Log = logger.CreateLogger(GetType());
            State = StreamProcessorStates.Paused;
        }

        protected override async Task ExecuteAsync(CancellationToken token) {
            _appToken = token;

            await InitializeAsync(token);
            await Monitor.MonitorAsync();

            if (Monitor.ShouldBeRunning) {
                await RunAsync();
                Log.LogDebug("Observing {@streamName}", _options.Stream);
            }

            token.Register(() => Dispose());

        }

        public async Task RunAsync() {
            State = StreamProcessorStates.Running;

            var remaining = int.MaxValue;
            var sliceStart = await ResolveLastCheckpointAsync() ?? 0;
            StreamEventsSlice currentSlice;
            bool isCompleted = false;

            do {
                if (_appToken.IsCancellationRequested) return;

                var page = remaining < _readPageSize ? remaining : _readPageSize;
                currentSlice = await _connection.ReadStreamEventsForwardAsync(
                    _options.Stream,
                    sliceStart,
                    page,
                    true);


                if (currentSlice is not StreamEventsSlice) { // can this be null-checked instead?
                    isCompleted = true;
                } else {
                    remaining -= currentSlice.Events.Length;
                    sliceStart = currentSlice.NextEventNumber;
                    var events = currentSlice.Events.Where(e => e.Event.IsJson).ToArray();
                    foreach (var e in events) {
                        await ProcessAsync(e);
                        OnProcessed?.Invoke(this, EventArgs.Empty);
                    }
                }

                if (currentSlice.NextEventNumber >= 0) {
                    await UpdateCheckpointAsync(currentSlice.NextEventNumber);
                }
            } while ((!currentSlice.IsEndOfStream && remaining != 0) || isCompleted);

            if (!_options.Continuous) {
                Log.LogDebug("Not observing stream for new events.  Cleaning up...");
                await CleanupAsync();
                return;
            }

            // setup subscription
            // TODO: Research to find out if there is a way to recover from a drop, or what best practices are.
            var checkPoint = await ResolveLastCheckpointAsync();
            if (checkPoint is not null) checkPoint -= 1;

            _subscription = _connection.SubscribeToStreamFrom(
                stream: _options.Stream,
                lastCheckpoint: checkPoint,
                settings: new CatchUpSubscriptionSettings(
                    maxLiveQueueSize: 10000,
                    readBatchSize: 500,
                    verboseLogging: false,
                    resolveLinkTos: true,
                    subscriptionName: $"{GetType().Name} => {_options.Stream}"),
                eventAppeared: async (sub, evt) => { 
                    await ProcessAsync(evt); 
                    await UpdateCheckpointAsync(evt.OriginalEventNumber);
                    Log.LogDebug("Event Type: {@EventType} - Position: {@Position}", evt.Event.EventType, evt.Event.EventNumber);
                },
                subscriptionDropped: (sub, reason, exc) => Log.LogWarning(exc, "Subscription dropped because: {@droppedReason}", reason)
            );
        }

        protected abstract Task InitializeAsync(CancellationToken token);
        protected abstract Task<long?> ResolveLastCheckpointAsync();
        protected abstract Task ProcessAsync(ResolvedEvent e);
        protected abstract Task UpdateCheckpointAsync(long position);
        protected abstract Task CleanupAsync();

        public Task PauseAsync() {
            State = StreamProcessorStates.Paused;
            _subscription?.Stop();
            _subscription = null;
            return Task.CompletedTask;
        }

        public override void Dispose() {
            base.Dispose();
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected async void Dispose(bool disposing) {
            if (_disposed) return; // nothing to do.

            if (!disposing) return;

            await CleanupAsync();

            _subscription?.Stop();
            _subscription = null;

            Monitor?.Dispose();
            // TODO: free unmanaged resources (unmanaged objects) and override finalizer

            // TODO: set large fields to null

            _disposed = true;
        }
    }
}
