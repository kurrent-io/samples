namespace EventStore.StreamConnectors.Redis {
    using System.Threading;
    using System.Threading.Tasks;

    using EventStore.ClientAPI;

    using Microsoft.Extensions.Logging;

    using RTI;

    using StackExchange.Redis;

    public abstract class RedisStreamProcessor : StreamProcessor {
        protected IDatabase Redis { get; private set; }
        protected RedisConfigurationOptions Options { get; private set; }

        public RedisStreamProcessor(IDatabase redis, RedisConfigurationOptions options, IEventStoreConnection connection, ILoggerFactory loggerFactory) : base(options, connection, loggerFactory) {
            Monitor = new StreamProcessorActivationMonitor(this, connection, loggerFactory, Backplanes.Direct);
            Redis = redis;
            Options = options;
        }

        protected override Task InitializeAsync(CancellationToken token) => Task.CompletedTask; // no initialization work.

        protected override Task CleanupAsync() => Task.CompletedTask; // no cleanup work.

        protected override async Task<long?> ResolveLastCheckpointAsync() => await Redis.HashExistsAsync(Strings.Collections.Checkpoints, Options.Stream)
            ? (long?)(await Redis.HashGetAsync(Strings.Collections.Checkpoints, Options.Stream))
            : null;

        protected override async Task UpdateCheckpointAsync(long position) => await Redis.HashSetAsync(Strings.Collections.Checkpoints, new[] { new HashEntry(Options.Stream, position) });

    }
}
