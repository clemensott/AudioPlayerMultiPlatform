using System;

namespace AudioPlayerFrontend.Join
{
    class WaveProvider : IWaveProvider
    {
        private readonly NAudio.Wave.WaveFormat format;

        public AudioPlayerBackend.Common.IWaveProvider Parent { get; private set; }

        public NAudio.Wave.WaveFormat WaveFormat { get { return format; } }

        AudioPlayerBackend.Common.WaveFormat AudioPlayerBackend.Common.IWaveProvider.WaveFormat { get { return Parent.WaveFormat; } }

        public TimeSpan CurrentTime { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public TimeSpan TotalTime => throw new NotImplementedException();

        public WaveProvider(AudioPlayerBackend.Common.IWaveProvider parent)
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
