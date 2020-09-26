using System;
using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    class AudioFileReader : IWaveProvider, IPositionWaveProvider
    {
        public TimeSpan CurrentTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TimeSpan TotalTime => throw new NotImplementedException();

        public WaveFormat WaveFormat => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
