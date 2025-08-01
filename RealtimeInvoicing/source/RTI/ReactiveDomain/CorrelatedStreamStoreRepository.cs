// ReSharper disable once CheckNamespace
namespace RTI.ReactiveDomain {
    using System;

    using global::ReactiveDomain;
    using global::ReactiveDomain.Foundation;
    using global::ReactiveDomain.Messaging;

    public class CorrelatedStreamStoreRepository : ICorrelatedRepository, IDisposable {
        private readonly IAggregateCache _cache;
        private readonly IRepository _repository;

        public CorrelatedStreamStoreRepository(
            IRepository repository,
            Func<IRepository, IAggregateCache> cacheFactory = null) {
            _repository = repository;
            if (cacheFactory != null) _cache = cacheFactory(_repository);
        }

        public CorrelatedStreamStoreRepository(
            IStreamNameBuilder streamNameBuilder,
            IStreamStoreConnection streamStoreConnection,
            IEventSerializer eventSerializer,
            Func<IRepository, IAggregateCache> cacheFactory = null) {
            _repository = new StreamStoreRepository(streamNameBuilder, streamStoreConnection, eventSerializer);
            if (cacheFactory != null) _cache = cacheFactory(_repository);
        }

        public bool TryGetById<TAggregate>(Guid id, out TAggregate aggregate, ICorrelatedMessage source) where TAggregate : AggregateRoot, IEventSource => TryGetById(id, int.MaxValue, out aggregate, source);

        public TAggregate GetById<TAggregate>(Guid id, ICorrelatedMessage source) where TAggregate : AggregateRoot, IEventSource => GetById<TAggregate>(id, int.MaxValue, source);

        public bool TryGetById<TAggregate>(Guid id, int version, out TAggregate aggregate, ICorrelatedMessage source) where TAggregate : AggregateRoot, IEventSource {
            try {
                aggregate = GetById<TAggregate>(id, version, source);
                return true;
            }
            catch (Exception) {
                aggregate = null;
                return false;
            }
        }

        public TAggregate GetById<TAggregate>(Guid id, int version, ICorrelatedMessage source) where TAggregate : AggregateRoot, IEventSource {
            var agg = _cache?.GetById<TAggregate>(id, version);

            if (agg == null || agg.Version > version) {
                agg = _repository.GetById<TAggregate>(id, version);
                if (agg != null) _cache?.Save(agg);
            }

            if (agg != null) ((ICorrelatedEventSource)agg).Source = source;
            return agg;
        }

        public void Save(IEventSource aggregate) {
            if (_cache != null)
                _cache.Save(aggregate);
            else
                _repository.Save(aggregate);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) _cache?.Dispose();
        }
    }
}