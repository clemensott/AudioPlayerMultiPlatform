namespace AudioPlayerFrontend.Join
{
    class WaveProvider : IWaveProvider
    {
        private readonly NAudio.Wave.WaveFormat format;

        public AudioPlayerBackend.Player.IWaveProvider Parent { get; private set; }

        public NAudio.Wave.WaveFormat WaveFormat => format;

        AudioPlayerBackend.Player.WaveFormat AudioPlayerBackend.Player.IWaveProvider.WaveFormat => Parent.WaveFormat;

        public WaveProvider(AudioPlayerBackend.Player.IWaveProvider parent)
        {
            Parent = parent;
            format = parent.WaveFormat.ToFrontend();
        }

        public void Dispose()
        {
            Parent.Dispose();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return Parent.Read(buffer, offset, count);
        }
    }
}
