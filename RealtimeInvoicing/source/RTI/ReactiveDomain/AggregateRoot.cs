namespace RTI.ReactiveDomain {
    using System;

    using global::ReactiveDomain;
    using global::ReactiveDomain.Messaging;
    using global::ReactiveDomain.Util;

    public abstract class AggregateRoot : EventDrivenStateMachine, ICorrelatedEventSource {
        private Guid _causationId;

        private Guid _correlationId;
        protected AggregateRoot() { }

        protected AggregateRoot(ICorrelatedMessage source = null) {
            if (source == null) return;
            _correlationId = source.CorrelationId;
            _causationId = source.MsgId;
        }

        ICorrelatedMessage ICorrelatedEventSource.Source {
            set {
                if (_correlationId != Guid.Empty && HasRecordedEvents)
                    throw new InvalidOperationException(
                        "Cannot change source unless there are no recorded events, or current source is null");
                _correlationId = value.CorrelationId;
                _causationId = value.MsgId;
            }
        }

        internal void RegisterChild(ChildEntity childAggregate, out Action<object> raise, out EventRouter router) {
            Ensure.NotNull(childAggregate, nameof(childAggregate));
            raise = Raise;
            router = Router;
        }


        protected override void TakeEventStarted() {
            if (HasRecordedEvents && _correlationId == Guid.Empty)
                throw new InvalidOperationException(
                    "Cannot take events without valid source.");
        }

        protected override void TakeEventsCompleted() {
            _correlationId = Guid.Empty;
            _causationId = Guid.Empty;
            base.TakeEventsCompleted();
        }

        protected override void OnEventRaised(object @event) {
            if (@event is ICorrelatedMessage correlatedEvent) {
                if (_correlationId == Guid.Empty)
                    throw new InvalidOperationException(
                        "Cannot raise events without valid source.");
                if (correlatedEvent.CorrelationId != Guid.Empty || correlatedEvent.CausationId != Guid.Empty) throw new InvalidOperationException("Cannot raise events with a different source.");
                correlatedEvent.CorrelationId = _correlationId;
                correlatedEvent.CausationId = _causationId;
            } else
                throw new InvalidOperationException("Cannot raise uncorrelated events from correlated Aggrgate.");

            base.OnEventRaised(@event);
        }
    }
}