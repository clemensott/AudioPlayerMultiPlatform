using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    class BufferedWaveProvider : NAudio.Wave.BufferedWaveProvider, IBufferedWaveProvider, IWaveProvider
    {
        private WaveFormat format;

        WaveFormat AudioPlayerBackend.Player.IWaveProvider.WaveFormat => format;

        public BufferedWaveProvider(WaveFormat format) : base(format.ToFrontend())
        {
            this.format = format;
        }

        public void Dispose()
        {
        }
    }
}
