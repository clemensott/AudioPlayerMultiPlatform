using System;
using AudioPlayerBackend.Common;

namespace AudioPlayerBackend
{
    public class ReadEventWaveProvider : IPositionWaveProvider
    {
        public IPositionWaveProvider Parent { get; private set; }

        internal event EventHandler<WaveProviderReadEventArgs> ReadEvent;

        public WaveFormat WaveFormat { get { return Parent.WaveFormat; } }

        public TimeSpan CurrentTime
        {
            get { return Parent.CurrentTime; }
            set { Parent.CurrentTime = value; }
        }

        internal ReadEventWaveProvider(IPositionWaveProvider parent)
        {
            this.Parent = parent;
        }

        public TimeSpan TotalTime { get { return Parent.TotalTime; } }

        public int Read(byte[] buffer, int offset, int count)
        {
            int readCount = Parent.Read(buffer, offset, count);

            var args = new WaveProviderReadEventArgs(buffer, offset, count, readCount);
            ReadEvent?.Invoke(this, args);

            return readCount;
        }

        public void Dispose()
        {
            Parent.Dispose();
        }
    }
}
