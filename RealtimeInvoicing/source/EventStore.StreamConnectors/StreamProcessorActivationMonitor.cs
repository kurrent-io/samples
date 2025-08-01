namespace EventStore.StreamConnectors {
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;

    using Microsoft.Extensions.Logging;

    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class StreamProcessorActivationMonitor : IDisposable {
        private readonly IProcessorControl _processor;
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly ILogger _logger;
        private readonly Backplanes _whenToRun;

        private EventStoreStreamCatchUpSubscription _subscription;

        public bool ShouldBeRunning { get; private set; }

        public StreamProcessorActivationMonitor(IProcessorControl processor, IEventStoreConnection eventStoreConnection, ILoggerFactory loggerFactory, Backplanes whenToRun) {
            _processor = processor;
            _eventStoreConnection = eventStoreConnection;
            _logger = loggerFactory.CreateLogger($"Stream Monitor - {processor.GetType().Name}");
            _whenToRun = whenToRun;
        }

        public async Task MonitorAsync() {
            _logger.LogTrace("Reading last event from stream.");
            var slice = await _eventStoreConnection.ReadStreamEventsBackwardAsync(
                KnownStreams.StreamProcessor,
                StreamPosition.End,
                1,
                true);
            if (slice is not StreamEventsSlice || slice.Events.Length == 0) { // can this be null-checked instead?
                await _processor.PauseAsync();
            } else {
                var signal = JsonSerializer.Deserialize<StreamProcessorSignal>(slice.Events[0].Event.Data);
                ShouldBeRunning = (signal?.ActiveBackplane ?? Backplanes.NotSet) == _whenToRun;
            }
            _logger.LogDebug("Should the service be running? {@ShouldBeRunning}", ShouldBeRunning ? "Yes" : "No");

            _subscription = _eventStoreConnection.SubscribeToStreamFrom(
                KnownStreams.StreamProcessor,
                slice.Events.Length == 0 ? null : slice.Events[0].Event.EventNumber,
                CatchUpSubscriptionSettings.Default,
                async (sub, e) => {
                    _logger.LogInformation("Processor state change request received.");

                    var signal = JsonSerializer.Deserialize<StreamProcessorSignal>(e.Event.Data);
                    _logger.LogDebug("Signal Active Backplane: {@ActiveBackplane}", signal.ActiveBackplane);
                    ShouldBeRunning = (signal?.ActiveBackplane ?? Backplanes.NotSet) == _whenToRun;
                    _logger.LogDebug("Should the service be running? {@ShouldBeRunning}", ShouldBeRunning ? "Yes" : "No");

                    if (ShouldBeRunning) {
                        if (_processor.State == StreamProcessorStates.Paused) {
                            _logger.LogInformation("Activating service.");
                            await _processor.RunAsync();
                        } else {
                            _logger.LogInformation("Service is already active.");
                        }
                    } else {
                        if (_processor.State == StreamProcessorStates.Running) {
                            _logger.LogInformation("Pausing service.");
                            await _processor.PauseAsync();
                        } else {
                            _logger.LogInformation("Service is already paused.");
                        }
                    }
                },
                subscriptionDropped: (sub, reason, exc) => _logger.LogWarning(exc, "Subscription dropped because: {@droppedReason}", reason));
        }

        public void Dispose() {
            _subscription?.Stop();
            _subscription = null;
        }
    }
}
