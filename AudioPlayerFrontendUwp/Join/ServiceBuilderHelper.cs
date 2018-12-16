using AudioPlayerBackend;

namespace AudioPlayerFrontend.Join
{
    class ServiceBuilderHelper : IServiceBuilderHelper
    {
        public IMqttAudioClient CreateAudioClient(IPlayer player, string serverAddress, int? port, ServiceBuilder builder)
        {
            return new MqttAudioClient(player, serverAddress, port, new AudioHelper());
        }

        public IMqttAudioService CreateAudioServer(IPlayer player, int port, ServiceBuilder builder)
        {
            return new MqttAudioService(player, port, new AudioHelper());
        }

        public IAudioExtended CreateAudioService(IPlayer player, ServiceBuilder builder)
        {
            return new AudioService(player, new AudioHelper());
        }
    }
}
