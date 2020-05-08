namespace AudioPlayerBackend.Communication
{
    interface IClientCommunicator
    {
        int? Port { get; }

        string ServerAddress { get; }
    }
}
