using System.Collections.Generic;
using AudioPlayerBackend;
using AudioPlayerBackend.Common;

namespace AudioPlayerFrontend.Join
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

        protected override IEnumerable<string> LoadFilePaths(string path)
        {
            return AudioService.LoadFilePathsStatic(path);
        }
    }
}
