using System;

namespace AudioPlayerBackend.Player
{
    public interface IWaveProvider : IDisposable
    {
        WaveFormat WaveFormat { get; }
        int Read(byte[] buffer, int offset, int count);
    }
}