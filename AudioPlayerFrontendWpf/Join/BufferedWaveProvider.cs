using AudioPlayerBackend.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerFrontendWpf.Join
{
    class BufferedWaveProvider : NAudio.Wave.BufferedWaveProvider, IBufferedWaveProvider
    {
        private WaveFormat format;
        WaveFormat IWaveProvider.WaveFormat { get { return format; } }

        public BufferedWaveProvider(WaveFormat format):base(format.ToFrontend())
        {
            this.format = format;
        }

        public void Dispose()
        {
        }
    }
}
