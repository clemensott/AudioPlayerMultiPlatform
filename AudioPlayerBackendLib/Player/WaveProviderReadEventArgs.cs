using System;

namespace AudioPlayerBackend.Player
{
    class WaveProviderReadEventArgs:EventArgs
    {
        public byte[] Buffer { get; private set; }

        public int Offset { get; private set; }

        public int ParmamCount { get; private set; }

        public int ReturnCount { get; private set; }

        public WaveProviderReadEventArgs(byte[] buffer, int offset, int parmamCount, int returnCount)
        {
            Buffer = buffer;
            Offset = offset;
            ParmamCount = parmamCount;
            ReturnCount = returnCount;
        }
    }
}
