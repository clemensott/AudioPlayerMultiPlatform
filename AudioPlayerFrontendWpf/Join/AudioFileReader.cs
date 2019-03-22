using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    class AudioFileReader : NAudio.Wave.AudioFileReader, IWaveProvider, IPositionWaveProvider
    {
        private readonly WaveFormat format;

        WaveFormat AudioPlayerBackend.Player.IWaveProvider.WaveFormat => format;

        public AudioFileReader(string path) : base(path)
        {
            format = WaveFormat.ToBackend();
        }
    }
}
