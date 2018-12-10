using AudioPlayerBackend;

namespace AudioPlayerFrontend.Join
{
    class ServiceBuilder : AudioPlayerBackend.ServiceBuilder
    {
        protected override IMqttAudioClient CreateAudioClient(IPlayer player, string serverAddress, int? port)
        {
            return new MqttAudioClient(player, serverAddress, port);
        }

        protected override IMqttAudioService CreateAudioServer(IPlayer player, int port)
        {
            return new MqttAudioService(player, port);
        }

        protected override IAudioExtended CreateAudioService(IPlayer player)
        {
            return new AudioService(player);
        }
    }
}
