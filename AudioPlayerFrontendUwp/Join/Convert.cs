using Backend = AudioPlayerBackend.Player;
using NAudio.Wave;
using System;

namespace AudioPlayerFrontend.Join
{
    static class Convert
    {
        public static Backend.PlaybackState ToBackend(this PlaybackState state)
        {
            return ToEnum<Backend.PlaybackState>(state);
        }

        public static Backend.WaveFormatEncoding ToBackend(this WaveFormatEncoding encoding)
        {
            Type type = typeof(Backend.WaveFormatEncoding);
            string name = Enum.GetName(typeof(WaveFormatEncoding), encoding);

            return ToEnum<Backend.WaveFormatEncoding>(encoding);
        }

        public static Backend.StoppedEventArgs ToBackend(this StoppedEventArgs args)
        {
            return new Backend.StoppedEventArgs(args.Exception);
        }

        public static Backend.WaveFormat ToBackend(this WaveFormat f)
        {
            return new Backend.WaveFormat(f.Encoding.ToBackend(),
                f.SampleRate, f.Channels, f.AverageBytesPerSecond, f.BlockAlign, f.BitsPerSample);
        }

        public static WaveFormatEncoding ToFrontend(this Backend.WaveFormatEncoding encoding)
        {
            Type type = typeof(WaveFormatEncoding);
            string name = Enum.GetName(typeof(Backend.WaveFormatEncoding), encoding);

            return (WaveFormatEncoding)Enum.Parse(type, name);
        }

        public static WaveFormat ToFrontend(this Backend.WaveFormat f)
        {
            return WaveFormat.CreateCustomFormat(f.Encoding.ToFrontend(),
                f.SampleRate, f.Channels, f.AverageBytesPerSecond, f.BlockAlign, f.BitsPerSample);
        }


        private static T ToEnum<T>(Enum value)
        {
            Type type = typeof(T);
            string name = Enum.GetName(value.GetType(), value);

            return (T)Enum.Parse(type, name);
        }
    }
}
