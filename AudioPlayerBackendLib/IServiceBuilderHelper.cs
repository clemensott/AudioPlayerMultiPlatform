using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication.MQTT;
using AudioPlayerBackend.Player;
using System;

namespace AudioPlayerBackend
{
    public interface IServiceBuilderHelper : INotifyPropertyChangedHelper
    {
        Func<IAudioService> CreateAudioService { get; }

        AudioStreamPlayer CreateAudioStreamPlayer(IWaveProviderPlayer player, IAudioService service);

        AudioServicePlayer CreateAudioServicePlayer(IWaveProviderPlayer player, IAudioService service);

        MqttClientCommunicator CreateMqttClientCommunicator(IAudioService service, string serverAddress, int? port);

        MqttServerCommunicator CreateMqttServerCommunicator(IAudioService service, int port);
    }
}
