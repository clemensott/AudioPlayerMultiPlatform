namespace AudioPlayerBackend.Common
{
    public interface IBufferedWaveProvider : IWaveProvider
    {
        void AddSamples(byte[] data, int offset, int count);
        void ClearBuffer();
    }
}
