using AudioPlayerBackend;
using AudioPlayerBackend.Common;

namespace AudioPlayerFrontendWpf.Join
{
    class MqttAudioClient : AudioPlayerBackend.MqttAudioClient
    {
        public MqttAudioClient(IPlayer player, string serverAddress, int? port = null) : base(player, serverAddress, port)
        {
        }

        protected override IBufferedWaveProvider CreateBufferedWaveProvider(WaveFormat format)
        {
            return new BufferedWaveProvider(format);
        }

        protected override IMqttClient CreateMqttClient()
        {
            return new MqttClient(new MQTTnet.MqttFactory().CreateMqttClient());
        }
    }
}
