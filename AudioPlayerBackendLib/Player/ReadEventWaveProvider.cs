using System;

namespace AudioPlayerBackend.Player
{
    public class ReadEventWaveProvider : IPositionWaveProvider
    {
        public IPositionWaveProvider Parent { get; private set; }

        internal event EventHandler<WaveProviderReadEventArgs> ReadEvent;

        public WaveFormat WaveFormat => Parent.WaveFormat;

        public TimeSpan CurrentTime
        {
            get => Parent.CurrentTime;
            set => Parent.CurrentTime = value;
        }

        internal ReadEventWaveProvider(IPositionWaveProvider parent)
        {
            this.Parent = parent;
        }

        public TimeSpan TotalTime => Parent.TotalTime;

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
