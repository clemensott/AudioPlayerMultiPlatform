using System;
using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication.MQTT;
using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    class ServiceBuilderHelper : NotifyPropertyChangedHelper, IServiceBuilderHelper
    {
        private static ServiceBuilderHelper instance;

        public static ServiceBuilderHelper Current
        {
            get
            {
                if (instance == null) instance = new ServiceBuilderHelper();

                return instance;
            }
        }

        public Func<IAudioService> CreateAudioService => DoCreateAudioService;

        private ServiceBuilderHelper() { }

        private IAudioService DoCreateAudioService()
        {
            return new AudioService(AudioServiceHelper.Current);
        }

        public AudioServicePlayer CreateAudioServicePlayer(IWaveProviderPlayer player, IAudioService service)
        {
            return new AudioServicePlayer(service, player, PlayerHelper.Current);
        }

        public AudioStreamPlayer CreateAudioStreamPlayer(IWaveProviderPlayer player, IAudioService service)
        {
            return new AudioStreamPlayer(service, player, PlayerHelper.Current);
        }

        public MqttClientCommunicator CreateMqttClientCommunicator(IAudioService service, string serverAddress, int? port)
        {
            return new MqttClientCommunicator(service, serverAddress, port);
        }

        public MqttServerCommunicator CreateMqttServerCommunicator(IAudioService service, int port)
        {
            return new MqttServerCommunicator(service, port);
        }
    }
}
