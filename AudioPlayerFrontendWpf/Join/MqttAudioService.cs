using AudioPlayerBackend;
using AudioPlayerBackend.Common;

namespace AudioPlayerFrontendWpf.Join
{
    class MqttAudioService : AudioPlayerBackend.MqttAudioService
    {
        public MqttAudioService(IPlayer player, int port) : base(player, port)
        {
        }

        protected override IMqttServer CreateMqttServer()
        {
            return new MqttServer(new MQTTnet.MqttFactory().CreateMqttServer());
        }

        protected override IPositionWaveProvider CreateWaveProvider(Song song)
        {
            return new AudioFileReader(song.FullPath);
        }
    }
}
