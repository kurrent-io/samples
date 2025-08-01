namespace RTI.ReactiveDomain {
    using System;
    using System.Collections.Generic;

    using global::ReactiveDomain;

    /// <summary>
    ///     The base class each process manager or aggregate's root entity should derive from.
    /// </summary>
    public abstract class EventDrivenStateMachine : IEventSource {
        private readonly EventRecorder _recorder;
        protected readonly EventRouter Router;

        /// <summary>
        ///     Initializes an event source's routing and recording behavior.
        /// </summary>
        protected EventDrivenStateMachine() {
            _recorder = new EventRecorder();
            Router = new EventRouter();
            Version = -1;
        }

        public bool HasRecordedEvents => _recorder.HasRecordedEvents;

        public long Version { get; private set; }

        public Guid Id { get; protected set; }

        long IEventSource.ExpectedVersion {
            get => Version;
            set => Version = value;
        }

        public void RestoreFromEvents(IEnumerable<object> events) {
            if (events == null)
                throw new ArgumentNullException(nameof(events));
            if (_recorder.HasRecordedEvents)
                throw new InvalidOperationException("Restoring from events is not possible when an instance has recorded events.");

            foreach (var @event in events) {
                if (Version < 0) // new aggregates have a expected version of -1 or -2
                    Version = 0; // got first event (zero based)
                else
                    Version++;
                Router.Route(@event);
            }
        }

        public void UpdateWithEvents(IEnumerable<object> events, long expectedVersion) {
            if (events == null)
                throw new ArgumentNullException(nameof(events));
            if (Version < 0)
                throw new InvalidOperationException("Updating with events is not possible when an instance has no historical events.");
            if (Version != expectedVersion) throw new InvalidOperationException("Expected version mismatch when updating ");

            foreach (var @event in events) {
                Version++;
                Router.Route(@event);
            }
        }

        /// <summary>
        ///     Returns all events from the EventRecorder since state was loaded or the last time TakeEvents was called
        ///     Clears the EventRecorder
        ///     Increment the Version/ExpectedVersion by the event count
        ///     After this operation additional events can be applied via RestoreFromEvents
        ///     Also Events can continue to be Raised either before or after this call.
        ///     TakeEventStarted will be called before the the Recorder is queried.
        ///     TakeEventCompleted will be called after the the Recorder is queried and cleared.
        /// </summary>
        /// <returns>Array of Object containing the Events Raised by the Aggregate since it was loaded or the last time TakeEvents was called</returns>
        public object[] TakeEvents() {
            TakeEventStarted();
            var records = _recorder.RecordedEvents;
            _recorder.Reset();
            Version += records.Length;
            TakeEventsCompleted();
            return records;
        }

        protected virtual void TakeEventStarted() { }
        protected virtual void TakeEventsCompleted() { }

        /// <summary>
        ///     Registers a route for the specified <typeparamref name="TEvent">type of event</typeparamref> to the logic that needs to be applied to this instance to support future behaviors.
        /// </summary>
        /// <typeparam name="TEvent">The type of event.</typeparam>
        /// <param name="route">The logic to route the event to.</param>
        protected void Register<TEvent>(Action<TEvent> route) => Router.RegisterRoute(route);

        /// <summary>
        ///     Registers a route for the specified <paramref name="typeOfEvent">type of event</paramref> to the logic that needs to be applied to this instance to support future behaviors.
        /// </summary>
        /// <param name="typeOfEvent">The type of event.</param>
        /// <param name="route">The logic to route the event to.</param>
        protected void Register(Type typeOfEvent, Action<object> route) => Router.RegisterRoute(typeOfEvent, route);

        protected virtual void OnEventRaised(object @event) { }

        /// <summary>
        ///     Raises the specified <paramref name="event" /> - applies it to this instance and records it in its history.
        /// </summary>
        /// <param name="event">The event to apply and record.</param>
        protected void Raise(object @event) {
            OnEventRaised(@event);
            Router.Route(@event);
            _recorder.Record(@event);
        }
    }
}