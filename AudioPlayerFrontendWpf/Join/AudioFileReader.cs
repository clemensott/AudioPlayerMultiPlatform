using AudioPlayerBackend.Common;

namespace AudioPlayerFrontendWpf.Join
{
    class AudioFileReader : NAudio.Wave.AudioFileReader, IPositionWaveProvider
    {
        private readonly WaveFormat format;

        WaveFormat IWaveProvider.WaveFormat { get { return format; } }

        public AudioFileReader(string path) : base(path)
        {
            format = WaveFormat.ToBackend();
        }
    }
}
