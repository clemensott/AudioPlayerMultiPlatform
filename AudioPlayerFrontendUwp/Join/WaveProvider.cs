using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    class WaveProvider : IWaveProvider
    {
        public WaveFormat WaveFormat => throw new System.NotImplementedException();

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
