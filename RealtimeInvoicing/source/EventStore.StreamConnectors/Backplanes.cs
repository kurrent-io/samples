namespace EventStore.StreamConnectors {
    public enum Backplanes {
        NotSet,  //STJ will not serialize "Default"
        Direct,
        Kafka
    }
}
