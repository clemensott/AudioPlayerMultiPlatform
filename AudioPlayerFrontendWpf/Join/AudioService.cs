using AudioPlayerBackend;
using AudioPlayerBackend.Common;

namespace AudioPlayerFrontendWpf.Join
{
    class AudioService : AudioPlayerBackend.AudioService
    {
        public AudioService(IPlayer player) : base(player)
        {
        }

        protected override IPositionWaveProvider CreateWaveProvider(Song song)
        {
            return new AudioFileReader(song.FullPath);
        }
    }
}
