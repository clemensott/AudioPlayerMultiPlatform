using AudioPlayerBackend.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerFrontendWpf.Join
{
    class WaveProvider : NAudio.Wave.IWaveProvider
    {
        private readonly NAudio.Wave.WaveFormat format;

        public IWaveProvider Parent { get; private set; }

        public NAudio.Wave.WaveFormat WaveFormat { get { return format; } }

        public WaveProvider(IWaveProvider parent)
        {
            this.Parent = parent;
            format = parent.WaveFormat.ToFrontend();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return Parent.Read(buffer, offset, count);
        }
    }
}
