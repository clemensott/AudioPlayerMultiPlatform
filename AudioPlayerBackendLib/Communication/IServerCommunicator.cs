namespace AudioPlayerBackend.Communication
{
    public interface IServerCommunicator : ICommunicator
    {
        int Port { get; }
    }
}
