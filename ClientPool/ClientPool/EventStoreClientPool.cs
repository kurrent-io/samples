namespace ClientPool {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using EventStore.Client;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class EventStoreClientPool {
        private SemaphoreSlim _readSemaphor;
        private SemaphoreSlim _writeSemaphor;
        private ReadNodeInformation _leader;
        private LinkedList<ReadNodeInformation> _readers;
        private LinkedList<ReadNodeInformation>.Enumerator _readersEnumerator;
        private EventStoreClientPoolOptions _options;
        private ILogger _log;

        public EventStoreClientPool(IOptions<EventStoreClientPoolOptions> options, ILoggerFactory loggerFactory) {
            _options = options.Value;
            _log = loggerFactory.CreateLogger<EventStoreClientPool>();
        }

        public Task ConnectAsync() {
            BuildLeaderConnection(_options.LeaderUri);

            var readNodeUris = _options.ReadNodeUris.Union(new[] { _options.LeaderUri }).ToArray();
            _readers = new LinkedList<ReadNodeInformation>(readNodeUris.Select(href => {
                var index = Array.IndexOf(readNodeUris, href);
                var settings = EventStoreClientSettings.Create($"{href}");
                settings.ConnectivitySettings.NodePreference = NodePreference.Random;
                settings.DefaultCredentials = _options.DefaultCredentials;
                var client = new EventStoreClient(settings);

                return new ReadNodeInformation(index, href, client);
            }).ToArray());

            var leaderSettings = EventStoreClientSettings.Create($"{_options.LeaderUri}");
            leaderSettings.ConnectivitySettings.NodePreference = NodePreference.Leader;
            leaderSettings.DefaultCredentials = _options.DefaultCredentials;
            _leader = new ReadNodeInformation(-1, _options.LeaderUri, new EventStoreClient(leaderSettings));

            _readSemaphor = new SemaphoreSlim(1);
            _readSemaphor.Release(1);

            _writeSemaphor = new SemaphoreSlim(0, _options.MaximumWriterThreads);
            _writeSemaphor.Release(_options.MaximumWriterThreads);
            _log.LogDebug("Writes will be done on Leader node: {@LeaderNodeUri}", _options.LeaderUri);

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<ResolvedEvent>> ReadStreamAsync(Direction direction, string streamName, StreamPosition revision, long maxCount = long.MaxValue, bool resolveLinkTos = false, TimeSpan? deadline = null, UserCredentials? userCredentials = null, CancellationToken token = default(CancellationToken)) {
            ResolvedEvent evt = default;
            List<ResolvedEvent> events = new();
            int numberOfRetries = 0;
            bool retriedLeaderRead = false;

            var startingNode = NextNode();
            var node = startingNode;

        // considered as bad ju-ju in certain circumstances, this is probably the clearest/cleanest way to express a retry.
        RETRY_READ:

            try {
                _log.LogTrace("Reading from: Node-{@ReadNode} - {@NodeUri}", node.ReadNodeIndex, node.ServerUri);
                var slice = node.Client.ReadStreamAsync(direction, streamName, revision, maxCount, resolveLinkTos, deadline, userCredentials, token);
                events.AddRange(await slice.ToListAsync(token));
            } catch (Exception exc) {
                numberOfRetries++;
                if (numberOfRetries == _readers.Count) throw;
                _log.LogWarning(exc, "Read failed for current node.  Retrying on next node.");
                node = NextNode();
                goto RETRY_READ;
            }

        RETRY_LEADER_READ:
            try {
                if (evt.Event != null) {
                    var slice = _leader.Client.ReadStreamAsync(direction, streamName, evt.OriginalEventNumber, maxCount, resolveLinkTos, deadline, userCredentials, token);
                    events.AddRange(await slice.ToListAsync());
                }
            } catch (NullReferenceException) {
                // squelch.
            } catch (NotLeaderException exc) {
                if (retriedLeaderRead) throw;
                retriedLeaderRead = true;
                BuildLeaderConnection(exc);
                goto RETRY_LEADER_READ;
            }

            return events;
        }
        private ReadNodeInformation NextNode() {
            _readSemaphor.Wait();

            try {
                if (_readersEnumerator.Current == null) {
                    _readersEnumerator = _readers.GetEnumerator();
                    _readersEnumerator.MoveNext();
                    return _readersEnumerator.Current;
                }

                if (_readersEnumerator.MoveNext()) return _readersEnumerator.Current;

                _readersEnumerator = _readers.GetEnumerator();
                _readersEnumerator.MoveNext();
                return _readersEnumerator.Current;
            } finally {
                _readSemaphor.Release();
            }
        }

        private void BuildLeaderConnection(NotLeaderException exc) {
            _log.LogWarning(exc, "Re-configuring leader to resolved node.");
            var llNode = _readers.First(x => x.ServerUri.Host == exc.LeaderEndpoint.Host && x.ServerUri.Port == exc.LeaderEndpoint.Port);
            if (llNode == null) throw exc;

            BuildLeaderConnection(llNode.ServerUri);
        }

        private void BuildLeaderConnection(Uri leaderUri) {
            _log.LogInformation("Setting leader connection to {@leaderUri}.", leaderUri);
            var grpcClientSettings = EventStoreClientSettings.Create($"{_options.LeaderUri}");
            grpcClientSettings.DefaultCredentials = _options.DefaultCredentials;
            grpcClientSettings.ConnectivitySettings.NodePreference = Client.NodePreference.Leader;
            _leader = new ReadNodeInformation(-1, leaderUri, new EventStoreClient(grpcClientSettings));
            _log.LogDebug("Lead node connection created.");
        }
    }
}