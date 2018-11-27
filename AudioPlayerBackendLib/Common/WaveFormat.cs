namespace AudioPlayerBackend.Common
{
    public class WaveFormat
    {
        public virtual int BlockAlign { get; private set; }

        public int AverageBytesPerSecond { get; private set; }

        public int SampleRate { get; private set; }

        public int Channels { get; private set; }

        public WaveFormatEncoding Encoding { get; private set; }

        public int ExtraSize { get; private set; }

        public int BitsPerSample { get; private set; }

        public WaveFormat(WaveFormatEncoding encoding, int sampleRate, int channels, int averageBytesPerSecond, int blockAlign, int bitsPerSample)
        {
            Encoding = encoding;
            SampleRate = sampleRate;
            Channels = channels;
            AverageBytesPerSecond = averageBytesPerSecond;
            BlockAlign = blockAlign;
            BitsPerSample = bitsPerSample;
        }
    }
}
