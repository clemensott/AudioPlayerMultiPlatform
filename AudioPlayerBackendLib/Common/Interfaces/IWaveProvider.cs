using System;

namespace AudioPlayerBackend.Common
{
    public interface IWaveProvider : IDisposable
    {
        WaveFormat WaveFormat { get; }
        int Read(byte[] buffer, int offset, int count);
    }
}