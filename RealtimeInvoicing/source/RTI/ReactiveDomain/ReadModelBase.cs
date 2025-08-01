namespace RTI.ReactiveDomain {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using global::ReactiveDomain;
    using global::ReactiveDomain.Foundation;
    using global::ReactiveDomain.Messaging;
    using global::ReactiveDomain.Messaging.Bus;
    using global::ReactiveDomain.Util;

    public abstract class ReadModelBase :
        IHandle<IMessage>,
        IPublisher,
        IDisposable {
        private readonly InMemoryBus _bus;
        private readonly Func<IListener> _getListener;
        private readonly List<IListener> _listeners;
        private readonly QueuedHandler _queue;
        private bool _disposed;

        protected ReadModelBase(string name, Func<IListener> getListener) {
            Ensure.NotNull(getListener, nameof(getListener));
            _getListener = getListener;
            _listeners = new List<IListener>();
            _bus = new InMemoryBus($"{nameof(ReadModelBase)}:{name} bus", false);
            _queue = new QueuedHandler(_bus, $"{nameof(ReadModelBase)}:{name} queue");
            _queue.Start();
        }

        public ISubscriber EventStream => _bus;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Handle(IMessage message) => ((IHandle<IMessage>)_queue).Handle(message);

        public void Publish(IMessage message) => ((IPublisher)_queue).Publish(message);

        private IListener AddNewListener() {
            var l = _getListener();
            lock (_listeners) {
                _listeners.Add(l);
            }

            l.EventStream.SubscribeToAll(_queue);
            return l;
        }

        public List<Tuple<string, long>> GetCheckpoint() {
            lock (_listeners) {
                return _listeners.Select(l => new Tuple<string, long>(l.StreamName, l.Position)).ToList();
            }
        }

        //QUESTION: Why is this public?
        public void Start(string stream, long? checkpoint = null, bool blockUntilLive = false, CancellationToken cancelWaitToken = default) => AddNewListener().Start(stream, checkpoint, blockUntilLive, cancelWaitToken);

        //QUESTION: Why is this public?
        public void Start<TAggregate>(Guid id, long? checkpoint = null, bool blockUntilLive = false, CancellationToken cancelWaitToken = default) where TAggregate : class, IEventSource => AddNewListener().Start<TAggregate>(id, checkpoint, blockUntilLive, cancelWaitToken);

        //QUESTION: Why is this public?
        public void Start<TAggregate>(long? checkpoint = null, bool blockUntilLive = false, CancellationToken cancelWaitToken = default) where TAggregate : class, IEventSource => AddNewListener().Start<TAggregate>(checkpoint, blockUntilLive, cancelWaitToken);


        protected virtual void Dispose(bool disposing) {
            if (_disposed) return;
            if (disposing) {
                lock (_listeners) {
                    _listeners?.ForEach(l => l?.Dispose());
                }

                _queue?.RequestStop();
                _bus?.Dispose();
            }

            _disposed = true;
        }
    }
}