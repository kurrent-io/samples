namespace EventStore.StreamConnectors {
    using System.Threading.Tasks;

    public interface IProcessorControl {
        StreamProcessorStates State { get; }
        Task PauseAsync();
        Task RunAsync();
    }
}
