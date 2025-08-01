namespace RTI.ReactiveDomain {
    using System;

    /// <summary>
    ///     While it might seem more natural to save and restore the event set behind the aggregate,
    ///     this cache stores only the collapsed state in the aggregate
    /// </summary>
    public interface IAggregateCache : IRepository, IDisposable {
        bool Remove(Guid id);
        void Clear();
    }
}