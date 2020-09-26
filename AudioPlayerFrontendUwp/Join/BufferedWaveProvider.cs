using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    class BufferedWaveProvider : IBufferedWaveProvider, IWaveProvider
    {
        public WaveFormat WaveFormat => throw new System.NotImplementedException();

        public void AddSamples(byte[] data, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public void ClearBuffer()
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }
    }
}
