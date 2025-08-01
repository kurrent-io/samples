namespace EventStore.StreamConnectors.Kafka {
    using System;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Confluent.Kafka;

    using EventStore.ClientAPI;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class KafkaStreamProcessor : BackgroundService, IProcessorControl {
        private readonly KafkaConfigurationOptions _options;
        private readonly IEventStoreConnection _connection;
        private readonly ILogger _log;
        private IProducer<string, byte[]> _producer;
        private CancellationToken _stoppingToken;
        private EventStorePersistentSubscriptionBase _subscription;
        private StreamProcessorActivationMonitor _monitor;

        public StreamProcessorStates State { get; private set; }

        public KafkaStreamProcessor(KafkaConfigurationOptions options, IEventStoreConnection connection, ILoggerFactory loggerFactory) {
            _monitor = new StreamProcessorActivationMonitor(this, connection, loggerFactory, Backplanes.Kafka);
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _log = loggerFactory.CreateLogger(GetType());
            State = StreamProcessorStates.Paused;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _stoppingToken = stoppingToken;
            await _monitor.MonitorAsync();

            if (_monitor.ShouldBeRunning) await RunAsync();

            stoppingToken.Register(() => {
                _subscription?.Stop(_options.DisconnectTimeout);

                _producer?.Flush();
                _producer?.Dispose();
                _log.LogDebug("Persistent Subscription and Producer have been stopped/disposed of respectively.");
            });
        }

        public async Task RunAsync() {
            State = StreamProcessorStates.Running;

            _producer = new ProducerBuilder<string, byte[]>(
                _options.ConnectionString
                    .Split(';')
                    .Select(x => x.Split('='))
                    .ToDictionary(x => x[0].Trim(), x => x[1].Trim())
            ).Build();
            _log.LogDebug("Producer has been constructed.");

            //TODO: create subscription if it does not exist.
            _subscription = await _connection.ConnectToPersistentSubscriptionAsync(
            _options.Stream,
            _options.Group,
            async (sub, e) => {
                try {
                    await ProcessAsync(e);
                    sub.Acknowledge(e);
                } catch (Exception ex) {
                    sub.Fail(e, PersistentSubscriptionNakEventAction.Stop, ex.Message);
                }
            },
            subscriptionDropped: (sub, reason, exc) => _log.LogWarning(exc, "Subscription Dropped: {@Reason}", reason),
            userCredentials: null,
            bufferSize: 10);
            _log.LogDebug("EventStore Persistent Subscription has been connected.");
        }

        public Task PauseAsync() {
            State = StreamProcessorStates.Paused;

            _subscription?.Stop(TimeSpan.FromSeconds(5));
            _subscription = null;

            _producer?.Dispose();
            _producer = null;

            return Task.CompletedTask;
        }

        protected async Task ProcessAsync(ResolvedEvent e) {
            var evt = e.Event;
            var x = new ProjectedEvent {
                EventStreamId = evt.EventStreamId,
                EventType = evt.EventType,
                Metadata = evt.Metadata,
                Position = e.Event.EventNumber,
                Data = Encoding.UTF8.GetString(evt.Data)
            };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(x);
            var msg = new Message<string, byte[]> { Key = string.Empty, Value = bytes };
            await _producer.ProduceAsync(_options.Topic, msg);
            _log.LogDebug("Event Type: {@EventType} - Position: {@Position}", e.Event.EventType, e.Event.EventNumber);
        }
    }
}
