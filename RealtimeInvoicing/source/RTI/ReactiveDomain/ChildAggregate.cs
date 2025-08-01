namespace RTI.ReactiveDomain {
    using System;

    using global::ReactiveDomain;

    public abstract class ChildEntity {
        private readonly Action<object> _raise;
        private readonly EventRouter _router;
        public readonly Guid Id;

        protected ChildEntity(
            Guid id,
            AggregateRoot root) {
            Id = id;
            root.RegisterChild(this, out _raise, out _router);
        }

        /// <summary>
        ///     Registers a route for the specified <typeparamref name="TEvent">type of event</typeparamref> to the logic that needs to be applied to this instance to support future behaviors.
        ///     n.b. A ChildAggregate will need to filter events by Id before applying
        /// </summary>
        /// <typeparam name="TEvent">The type of event.</typeparam>
        /// <param name="route">The logic to route the event to.</param>
        protected void Register<TEvent>(Action<TEvent> route) => _router.RegisterRoute(route);

        /// <summary>
        ///     Registers a route for the specified <paramref name="typeOfEvent">type of event</paramref> to the logic that needs to be applied to this instance to support future behaviors.
        ///     n.b. A ChildAggregate will need to filter events by Id before applying
        /// </summary>
        /// <param name="typeOfEvent">The type of event.</param>
        /// <param name="route">The logic to route the event to.</param>
        protected void Register(Type typeOfEvent, Action<object> route) => _router.RegisterRoute(typeOfEvent, route);

        protected void Raise(object @event) => _raise(@event);
    }
}