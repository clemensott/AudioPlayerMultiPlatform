namespace AudioPlayerFrontend.Join
{
    class WaveProvider : IWaveProvider
    {
        public AudioPlayerBackend.Player.IWaveProvider Parent { get; private set; }

        public NAudio.Wave.WaveFormat WaveFormat { get; }

        AudioPlayerBackend.Player.WaveFormat AudioPlayerBackend.Player.IWaveProvider.WaveFormat => Parent.WaveFormat;

        public WaveProvider(AudioPlayerBackend.Player.IWaveProvider parent)
        {
            Parent = parent;
            WaveFormat = parent.WaveFormat.ToFrontend();
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
