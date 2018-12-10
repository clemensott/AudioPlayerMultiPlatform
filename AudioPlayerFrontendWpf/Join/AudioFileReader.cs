using AudioPlayerBackend.Common;

namespace AudioPlayerFrontend.Join
{
    class AudioFileReader : NAudio.Wave.AudioFileReader, IWaveProvider, IPositionWaveProvider
    {
        private readonly WaveFormat format;

        WaveFormat AudioPlayerBackend.Common.IWaveProvider.WaveFormat { get { return format; } }

        public AudioFileReader(string path) : base(path)
        {
            format = WaveFormat.ToBackend();
        }
    }
}
