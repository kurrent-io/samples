namespace RTI.ReactiveDomain {
    using System;
    using System.Collections.Generic;

    using global::ReactiveDomain;

    /// <summary>
    ///     This implementation assumes that the cached aggregate may have changes since it was last seen
    ///     Therefore the aggregate is cached on first retrieve and the store is checked for updates upon
    ///     each subsequent retrieval
    ///     Also retrieving an aggregate at anything other than latest is disabled
    ///     Cache management (e.g. eviction) is the responsibility of the caller/external owner
    ///     Save failures will clear the aggregate from the cache and return false
    /// </summary>
    public class ReadThroughAggregateCache : IAggregateCache, IDisposable {
        private readonly IRepository _baseRepository;
        private readonly Dictionary<Guid, IEventSource> _knownAggregates = new();

        public ReadThroughAggregateCache(IRepository baseRepository) => _baseRepository = baseRepository;

        public bool TryGetById<TAggregate>(Guid id, out TAggregate aggregate, int version = int.MaxValue) where TAggregate : class, IEventSource {
            try {
                aggregate = GetById<TAggregate>(id, version);
                return true;
            }
            catch (Exception) {
                aggregate = null;
                Remove(id);
                return false;
            }
        }

        public TAggregate GetById<TAggregate>(Guid id, int version = int.MaxValue) where TAggregate : class, IEventSource {
            if (_knownAggregates.TryGetValue(id, out var cached)) {
                var agg = (TAggregate)cached;
                Update(ref agg, version);
                return (TAggregate)cached;
            }

            var aggregate = _baseRepository.GetById<TAggregate>(id, version);
            _knownAggregates.Add(id, aggregate);
            return aggregate;
        }

        public void Update<TAggregate>(ref TAggregate aggregate, int version = int.MaxValue) where TAggregate : class, IEventSource {
            if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
            if (aggregate.ExpectedVersion == version) return;

            _knownAggregates.TryGetValue(aggregate.Id, out var cached);

            if (cached == null) {
                _baseRepository.Update(ref aggregate, version);
                UpdateCache(aggregate);
                return;
            }

            if (cached.ExpectedVersion == version) {
                aggregate = (TAggregate)cached;
                return;
            }

            if (cached.ExpectedVersion > version) {
                //cache is ahead of requested version, we need to get it fresh from repo
                _baseRepository.Update(ref aggregate, version);
                //don't regress the cache, just return
                return;
            }

            //cache is ahead of item, but behind requested version
            aggregate = (TAggregate)cached;
            _baseRepository.Update(ref aggregate, version);
            UpdateCache(aggregate);
        }

        public void Save(IEventSource aggregate) {
            try {
                _baseRepository.Save(aggregate);
                if (!_knownAggregates.ContainsKey(aggregate.Id))
                    _knownAggregates.Add(aggregate.Id, aggregate);
                else
                    _knownAggregates[aggregate.Id] = aggregate;
            }
            catch {
                _knownAggregates.Remove(aggregate.Id);
            }
        }

        public bool Remove(Guid id) => _knownAggregates.Remove(id);

        public void Clear() => _knownAggregates.Clear();

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        private void UpdateCache(IEventSource aggregate) {
            if (_knownAggregates.TryGetValue(aggregate.Id, out var cached)) {
                if (cached.ExpectedVersion < aggregate.ExpectedVersion) _knownAggregates[aggregate.Id] = aggregate;
            } else
                _knownAggregates.Add(aggregate.Id, aggregate);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) _knownAggregates.Clear();
        }
    }
}