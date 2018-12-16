using AudioPlayerBackend.Common;

namespace AudioPlayerBackend
{
    public interface IMqttAudioClientHelper : IAudioClientHelper
    {
        IMqttClient CreateMqttClient(IMqttAudioClient client);

        IBufferedWaveProvider CreateBufferedWaveProvider(WaveFormat format, IMqttAudioClient client);
    }
}
