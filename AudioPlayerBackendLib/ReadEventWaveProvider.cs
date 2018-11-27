using System;
using AudioPlayerBackend.Common;

namespace AudioPlayerBackend
{
    class ReadEventWaveProvider : IWaveProvider
    {
        private IWaveProvider parent;

        public event EventHandler<WaveProviderReadEventArgs> ReadEvent;

        public ReadEventWaveProvider(IWaveProvider parent)
        {
            this.parent = parent;
        }

        public WaveFormat WaveFormat { get { return parent.WaveFormat; } }

        public int Read(byte[] buffer, int offset, int count)
        {
            int readCount = parent.Read(buffer, offset, count);

            var args = new WaveProviderReadEventArgs(buffer, offset, count, readCount);
            ReadEvent?.Invoke(this, args);

            return readCount;
        }

        public void Dispose()
        {
            parent.Dispose();
        }
    }
}
