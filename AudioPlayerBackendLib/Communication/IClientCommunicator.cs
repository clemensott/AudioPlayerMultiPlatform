namespace AudioPlayerBackend.Communication
{
    public interface IClientCommunicator
    {
        int? Port { get; }

        string ServerAddress { get; }
    }
}
