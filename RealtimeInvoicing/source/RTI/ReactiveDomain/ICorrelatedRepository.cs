namespace RTI.ReactiveDomain {
    using System;

    using global::ReactiveDomain;
    using global::ReactiveDomain.Messaging;

    public interface ICorrelatedRepository {
        bool TryGetById<TAggregate>(Guid id, out TAggregate aggregate, ICorrelatedMessage source) where TAggregate : AggregateRoot, IEventSource;
        bool TryGetById<TAggregate>(Guid id, int version, out TAggregate aggregate, ICorrelatedMessage source) where TAggregate : AggregateRoot, IEventSource;
        TAggregate GetById<TAggregate>(Guid id, ICorrelatedMessage source) where TAggregate : AggregateRoot, IEventSource;
        TAggregate GetById<TAggregate>(Guid id, int version, ICorrelatedMessage source) where TAggregate : AggregateRoot, IEventSource;
        void Save(IEventSource aggregate);
    }
}