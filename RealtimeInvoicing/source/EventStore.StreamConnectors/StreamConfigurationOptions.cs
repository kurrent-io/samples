namespace EventStore.StreamConnectors {
    public abstract class StreamConfigurationOptions {
        /// <summary>
        /// The source or destination stream within EventStore DB
        /// </summary>
        public string Stream { get; set; }

        /// <summary>
        /// If true, then setup a subscription against the stream to automatically push events to the external source.
        /// </summary>
        public bool Continuous { get; set; }

        /// <summary>
        /// Future Use
        /// </summary>
        public StreamDirection Direction { get; set; } = StreamDirection.ToExternalSource;
        
        /// <summary>
        /// If true, then the property `Id` is set to the current stream name.  if false, then the field is mapped.
        /// </summary>
        public bool UsesStreamNameAsKey { get; set; } = true;
    }
}
