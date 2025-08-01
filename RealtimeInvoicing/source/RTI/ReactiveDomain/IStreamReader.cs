namespace RTI.ReactiveDomain {
    using System;

    using global::ReactiveDomain;
    using global::ReactiveDomain.Messaging.Bus;

    public interface IStreamReader : IDisposable {
        /// <summary>
        ///     The Eventstream the Events are read onto
        /// </summary>
        ISubscriber EventStream { get; }

        /// <summary>
        ///     The ending position of the stream after the read is complete
        /// </summary>
        long? Position { get; }

        /// <summary>
        ///     The name of the stream being read
        /// </summary>
        string StreamName { get; }

        /// <summary>
        ///     Reads the events on a named stream
        /// </summary>
        /// <param name="stream">the exact stream name</param>
        /// <param name="checkpoint">start point to listen from</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="readBackwards">read the stream backwards</param>
        /// <returns>Returns true if any events were read from the stream</returns>
        bool Read(string stream, long? checkpoint = null, long? count = null, bool readBackwards = false);

        /// <summary>
        ///     By Event Type Projection Reader
        ///     i.e. $et-[MessageType]
        /// </summary>
        /// <param name="tMessage">The message type used to generate the stream (projection) name</param>
        /// <param name="checkpoint">The starting point to read from.</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="readBackwards">Read the stream backwards</param>
        /// <returns>Returns true if any events were read from the stream</returns>
        bool Read(Type tMessage, long? checkpoint = null, long? count = null, bool readBackwards = false);

        /// <summary>
        ///     Reads the events on an aggregate root stream
        /// </summary>
        /// <typeparam name="TAggregate">The type of aggregate</typeparam>
        /// <param name="id">the aggregate id</param>
        /// <param name="checkpoint">start point to listen from</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="readBackwards">read the stream backwards</param>
        /// <returns>Returns true if any events were read from the stream</returns>
        bool Read<TAggregate>(Guid id, long? checkpoint = null, long? count = null, bool readBackwards = false) where TAggregate : class, IEventSource;

        /// <summary>
        ///     Reads the events on a Aggregate Category Stream
        /// </summary>
        /// <typeparam name="TAggregate">The type of aggregate</typeparam>
        /// <param name="checkpoint">start point to listen from</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="readBackwards">read the stream backwards</param>
        /// <returns>Returns true if any events were read from the stream</returns>
        bool Read<TAggregate>(long? checkpoint = null, long? count = null, bool readBackwards = false) where TAggregate : class, IEventSource;


        /// <summary>
        ///     Interrupts the reading process. Doesn't guarantee the moment when reading is stopped. For optimization purpose.
        /// </summary>
        void Cancel();
    }
}