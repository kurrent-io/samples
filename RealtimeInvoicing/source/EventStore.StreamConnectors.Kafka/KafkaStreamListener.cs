namespace EventStore.StreamConnectors.Kafka {
    using System;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Confluent.Kafka;

    using EventStore.ClientAPI;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public abstract class KafkaStreamListener : BackgroundService, IProcessorControl {
        protected IKafkaOptions Options { get; private set; }

        public StreamProcessorStates State { get; private set; }

        protected readonly ILogger Log;
        private IConsumer<string, byte[]> _consumer;
        private IEventStoreConnection _esConnection;
        private readonly int _readPageSize = 500;
        StreamProcessorActivationMonitor _monitor;
        private CancellationTokenSource _cts = new();

        public KafkaStreamListener(IKafkaOptions options, IEventStoreConnection esConnection, ILoggerFactory loggerFactory) {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Log = loggerFactory.CreateLogger(GetType());
            _esConnection = esConnection ?? throw new ArgumentNullException(nameof(esConnection));
            _monitor = new StreamProcessorActivationMonitor(this, esConnection, loggerFactory, Backplanes.Kafka);
            State = StreamProcessorStates.Paused;
        }

        protected abstract Task InitializeAsync();

        protected override async Task ExecuteAsync(CancellationToken token) {
            await InitializeAsync();
            await _monitor.MonitorAsync();

            if (_monitor.ShouldBeRunning) {
                await RunAsync();
                Log.LogDebug("Observing {@streamName}", Options.Topic);
            }

            token.Register(async () => await CleanupAsync());
        }

        public async Task RunAsync() {
            State = StreamProcessorStates.Running;
            var dict = Options.ConnectionString
                .Split(";")
                .Select(x => x.Split("="))
                .ToDictionary(x => x[0].Trim(), x => x[1].Trim());
            dict.Add("group.id", Options.Group);

            ConsumeResult<string, byte[]> cr = null;
            while (!_cts.Token.IsCancellationRequested && _monitor.ShouldBeRunning) {
                try {
                    if(_consumer == null) {
                        _consumer = new ConsumerBuilder<string, byte[]>(dict).Build();
                        _consumer.Subscribe(Options.Topic);
                        Log.LogDebug("Consumer has been constructed");
                    }

                    try {
                        cr = _consumer?.Consume(_cts.Token);
                    } catch {
                        _consumer?.Dispose();
                        _consumer = null;
                        continue;
                    }
                    if (cr == null || _cts.Token.IsCancellationRequested) return;

                    var projected = JsonSerializer.Deserialize<ProjectedEvent>(cr.Message.Value);
                    if (string.IsNullOrWhiteSpace(projected?.Data)) continue;

                    // determine whether to drop, catch-up, or upsert.

                    var committedPos = await ResolveLastCheckpointAsync();
                    if (committedPos == projected.Position) {
                        Log.LogDebug("Dropping event - later message received already. Committed: {@CommittedPos} Projected: {@ProjectedPos}", committedPos, projected.Position);
                        continue;
                    }

                    if (committedPos == (projected.Position - 1)) {
                        Log.LogDebug("Processing event - next in line. Committed: {@CommittedPos} Projected: {@ProjectedPos}", committedPos, projected.Position);
                        await ProcessAsync(projected);
                        _consumer?.Commit(cr);
                        continue;
                    }

                    // need to catch-up before reading next message.
                    Log.LogDebug("Catching up - Received event is newer than last known event. Committed: {@CommittedPos} Projected: {@ProjectedPos}", committedPos, projected.Position);
                    await CatchUpAsync(projected.Position);
                } catch (ConsumeException exc) {
                    Log.LogWarning(exc, "Consuming an event caused an exception.");
                    throw;
                }
            }
        }

        protected abstract Task ProcessAsync(ProjectedEvent e);
        protected abstract Task CleanupAsync();
        protected abstract Task<long?> ResolveLastCheckpointAsync();
        protected abstract Task UpdateCheckpointAsync(long position);

        private async Task CatchUpAsync(long toCheckpoint) {
            var remaining = int.MaxValue;
            var sliceStart = await ResolveLastCheckpointAsync() ?? 0;
            StreamEventsSlice currentSlice;
            bool isCompleted = false;

            do {
                var page = remaining < _readPageSize ? remaining : _readPageSize;
                currentSlice = await _esConnection.ReadStreamEventsForwardAsync(
                    Options.Stream,
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
                        await ProcessAsync(new ProjectedEvent {
                            Data = Encoding.UTF8.GetString(e.Event.Data),
                            EventStreamId = e.Event.EventStreamId,
                            EventType = e.Event.EventType,
                            Metadata = e.Event.Metadata,
                            Position = e.Event.EventNumber
                        });

                        if (e.Event.EventNumber >= toCheckpoint) {
                            await UpdateCheckpointAsync(currentSlice.NextEventNumber);
                            return;
                        }
                    }
                }

                await UpdateCheckpointAsync(currentSlice.NextEventNumber);
            } while ((!currentSlice.IsEndOfStream && remaining != 0) || isCompleted);
        }

        public Task PauseAsync() {
            _cts.Cancel();

            State = StreamProcessorStates.Paused;
            _consumer?.Dispose();
            _consumer = null;

            Log.LogDebug("Consumer has been paused: {@ConsumerName}", GetType().Name);

            _cts.TryReset();

            return Task.CompletedTask;
        }

        public async Task ResumeAsync() => await RunAsync();
    }
}
