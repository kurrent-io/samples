namespace RTI.ReactiveDomain {
    using System;
    using System.Threading;

    using global::ReactiveDomain;
    using global::ReactiveDomain.Foundation;
    using global::ReactiveDomain.Messaging;
    using global::ReactiveDomain.Messaging.Bus;
    using global::ReactiveDomain.Util;

    /// <summary>
    ///     StreamReader
    ///     This class reads streams on a Subscribable bus and is primarily used in the building of read models.
    ///     The Raw events returned from the Stream will be unwrapped using the provided serializer and
    ///     consumers can subscribe to event notifications by subscribing to the exposed EventStream.
    /// </summary>
    public class StreamReader : IStreamReader {
        private const int ReadPageSize = 500;
        private readonly IStreamNameBuilder _streamNameBuilder;
        private readonly IStreamStoreConnection _streamStoreConnection;
        protected readonly InMemoryBus Bus;
        protected readonly string ReaderName;
        protected readonly IEventSerializer Serializer;

        private bool _cancelled;
        protected bool firstEventRead;
        protected long StreamPosition;

        /// <summary>
        ///     Create a stream Reader
        /// </summary>
        /// <param name="name">Name of the reader</param>
        /// <param name="streamStoreConnection">The stream store to subscribe to</param>
        /// <param name="streamNameBuilder">The source for correct stream names based on aggregates and events</param>
        /// <param name="serializer">the serializer to apply to the evenets in the stream</param>
        /// <param name="busName">The name to use for the internal bus (helpful in debugging)</param>
        public StreamReader(
            string name,
            IStreamStoreConnection streamStoreConnection,
            IStreamNameBuilder streamNameBuilder,
            IEventSerializer serializer,
            IHandle<IMessage> target = null,
            string busName = null) {
            ReaderName = name ?? nameof(StreamReader);
            _streamStoreConnection = streamStoreConnection ?? throw new ArgumentNullException(nameof(streamStoreConnection));
            _streamNameBuilder = streamNameBuilder ?? throw new ArgumentNullException(nameof(streamNameBuilder));
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Bus = new InMemoryBus(busName ?? $"{ReaderName} {nameof(EventStream)}");
            if (target != null) Bus.SubscribeToAll(target);
        }

        public ISubscriber EventStream => Bus;
        public long? Position => firstEventRead ? StreamPosition : null;
        public string StreamName { get; private set; }

        /// <summary>
        ///     By Event Type Projection Reader
        ///     i.e. $et-[MessageType]
        /// </summary>
        /// <param name="tMessage">The message type used to generate the stream (projection) name</param>
        /// <param name="checkpoint">The starting point to read from.</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="readBackwards">Read the stream backwards</param>
        /// <returns>Returns true if any events were read from the stream</returns>
        /// <exception cref="ArgumentException"><paramref name="tMessage" /> must implement IMessage</exception>
        public bool Read(
            Type tMessage,
            long? checkpoint = null,
            long? count = null,
            bool readBackwards = false) {
            if (!typeof(IMessage).IsAssignableFrom(tMessage)) throw new ArgumentException("tMessage must implement IMessage", nameof(tMessage));

            return Read(
                _streamNameBuilder.GenerateForEventType(tMessage.Name),
                checkpoint,
                count,
                readBackwards);
        }

        /// <summary>
        ///     By Category Projection Stream Reader
        ///     i.e. $ce-[AggregateType]
        /// </summary>
        /// <typeparam name="TAggregate">The Aggregate type used to generate the stream name</typeparam>
        /// <param name="checkpoint">The starting point to read from.</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="readBackwards">Read the stream backwards</param>
        /// <returns>Returns true if any events were read from the stream</returns>
        public bool Read<TAggregate>(
            long? checkpoint = null,
            long? count = null,
            bool readBackwards = false) where TAggregate : class, IEventSource =>
            Read(
                _streamNameBuilder.GenerateForCategory(typeof(TAggregate)),
                checkpoint,
                count,
                readBackwards);


        /// <summary>
        ///     Aggregate-[id] Stream Reader
        ///     i.e. [AggregateType]-[id]
        /// </summary>
        /// <typeparam name="TAggregate">The Aggregate type used to generate the stream name</typeparam>
        /// <param name="id">Aggregate id to generate stream name.</param>
        /// <param name="checkpoint">The starting point to read from.</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="readBackwards">Read the stream backwards</param>
        /// <returns>Returns true if any events were read from the stream</returns>
        public bool Read<TAggregate>(
            Guid id,
            long? checkpoint = null,
            long? count = null,
            bool readBackwards = false) where TAggregate : class, IEventSource =>
            Read(
                _streamNameBuilder.GenerateForAggregate(typeof(TAggregate), id),
                checkpoint,
                count,
                readBackwards);

        /// <summary>
        ///     Named Stream Reader
        ///     i.e. [StreamName]
        /// </summary>
        /// <param name="streamName">An exact stream name.</param>
        /// <param name="checkpoint">The starting point to read from.</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="readBackwards">Read the stream backwards</param>
        /// <returns>Returns true if any events were read from the stream</returns>
        public virtual bool Read(
            string streamName,
            long? checkpoint = null,
            long? count = null,
            bool readBackwards = false) {
            var eventsRead = false;

            if (checkpoint != null)
                Ensure.Nonnegative((long)checkpoint, nameof(checkpoint));
            if (count != null)
                Ensure.Positive((long)count, nameof(count));
            if (!ValidateStreamName(streamName))
                throw new ArgumentException("Stream not found.", streamName);

            _cancelled = false;
            firstEventRead = false;
            StreamName = streamName;
            var sliceStart = checkpoint ?? (readBackwards ? -1 : 0);
            var remaining = count ?? long.MaxValue;
            StreamEventsSlice currentSlice;

            do {
                var page = remaining < ReadPageSize ? remaining : ReadPageSize;

                currentSlice = !readBackwards
                    ? _streamStoreConnection.ReadStreamForward(streamName, sliceStart, page)
                    : _streamStoreConnection.ReadStreamBackward(streamName, sliceStart, page);

                if (!(currentSlice is StreamEventsSlice)) return false;

                remaining -= currentSlice.Events.Length;
                sliceStart = currentSlice.NextEventNumber;

                Array.ForEach(currentSlice.Events, EventRead);
            } while (!currentSlice.IsEndOfStream && !_cancelled && remaining != 0);

            return eventsRead;
        }

        public void Cancel() => _cancelled = true;

        public bool ValidateStreamName(string streamName) {
            var currentSlice = _streamStoreConnection.ReadStreamForward(streamName, 0, 1);
            return !(currentSlice is StreamDeletedSlice);
        }

        protected virtual void EventRead(RecordedEvent recordedEvent) {
            // do not publish or increase counters if cancelled
            if (_cancelled) return;

            Interlocked.Exchange(ref StreamPosition, recordedEvent.EventNumber);
            firstEventRead = true;

            if (Serializer.Deserialize(recordedEvent) is IMessage @event) Bus.Publish(@event);
        }

#region Implementation of IDisposable

        private bool _disposed;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (_disposed)
                return;
            Bus?.Dispose();
            _disposed = true;
        }

#endregion
    }
}