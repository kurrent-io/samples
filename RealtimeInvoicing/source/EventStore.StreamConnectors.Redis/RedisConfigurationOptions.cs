namespace EventStore.StreamConnectors.Redis {
    public class RedisConfigurationOptions : StreamConfigurationOptions {
        public string KeyPrefix { get; set; }
    }
}
