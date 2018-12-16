namespace AudioPlayerBackend
{
    public interface IServiceBuilderHelper
    {
        IMqttAudioClient CreateAudioClient(IPlayer player, string serverAddress, int? port, ServiceBuilder builder);

        IMqttAudioService CreateAudioServer(IPlayer player, int port, ServiceBuilder builder);

        IAudioExtended CreateAudioService(IPlayer player, ServiceBuilder builder);
    }
}
