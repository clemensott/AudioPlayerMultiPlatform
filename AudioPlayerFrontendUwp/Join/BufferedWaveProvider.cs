using AudioPlayerBackend.Common;

namespace AudioPlayerFrontend.Join
{
    class BufferedWaveProvider : NAudio.Wave.BufferedWaveProvider, IBufferedWaveProvider, IWaveProvider
    {
        private WaveFormat format;

        WaveFormat AudioPlayerBackend.Common.IWaveProvider.WaveFormat { get { return format; } }

        public BufferedWaveProvider(WaveFormat format) : base(format.ToFrontend())
        {
            this.format = format;
        }

        public void Dispose()
        {
        }
    }
}
