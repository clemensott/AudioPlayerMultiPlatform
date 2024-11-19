namespace AudioPlayerBackend.Communication
{
    public interface IClientCommunicator : ICommunicator
    {
        int? Port { get; }

        string ServerAddress { get; }
    }
}
