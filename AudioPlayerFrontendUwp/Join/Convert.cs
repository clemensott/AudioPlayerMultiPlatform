using Common = AudioPlayerBackend.Common;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using NAudio.Wave;
using System;

namespace AudioPlayerFrontend.Join
{
    static class Convert
    {
        public static Common.PlaybackState ToBackend(this PlaybackState state)
        {
            return ToEnum<Common.PlaybackState>(state);
        }

        public static Common.WaveFormatEncoding ToBackend(this WaveFormatEncoding encoding)
        {
            Type type = typeof(Common.WaveFormatEncoding);
            string name = Enum.GetName(typeof(WaveFormatEncoding), encoding);

            return ToEnum<Common.WaveFormatEncoding>(encoding);
        }

        public static Common.StoppedEventArgs ToBackend(this StoppedEventArgs args)
        {
            return new Common.StoppedEventArgs(args.Exception);
        }

        public static Common.MqttApplicationMessageReceivedEventArgs ToBackend(this MqttApplicationMessageReceivedEventArgs e)
        {
            return new Common.MqttApplicationMessageReceivedEventArgs(e.ApplicationMessage.ToBackend());
        }

        public static Common.WaveFormat ToBackend(this WaveFormat f)
        {
            return new Common.WaveFormat(f.Encoding.ToBackend(),
                f.SampleRate, f.Channels, f.AverageBytesPerSecond, f.BlockAlign, f.BitsPerSample);
        }

        public static WaveFormatEncoding ToFrontend(this Common.WaveFormatEncoding encoding)
        {
            Type type = typeof(WaveFormatEncoding);
            string name = Enum.GetName(typeof(Common.WaveFormatEncoding), encoding);

            return (WaveFormatEncoding)Enum.Parse(type, name);
        }

        public static Common.MqttQualityOfServiceLevel ToBackend(this MqttQualityOfServiceLevel qos)
        {
            return ToEnum<Common.MqttQualityOfServiceLevel>(qos);
        }

        public static Common.MqttApplicationMessage ToBackend(this MqttApplicationMessage message)
        {
            return new Common.MqttApplicationMessage()
            {
                Payload = message.Payload,
                QualityOfServiceLevel = message.QualityOfServiceLevel.ToBackend(),
                Retain = message.Retain,
                Topic = message.Topic
            };
        }

        public static Common.MqttApplicationMessageInterceptorContext ToBackend(this MqttApplicationMessageInterceptorContext c)
        {
            return new Common.MqttApplicationMessageInterceptorContext(c.ClientId, c.ApplicationMessage.ToBackend(), c.AcceptPublish);
        }

        public static WaveFormat ToFrontend(this Common.WaveFormat f)
        {
            return WaveFormat.CreateCustomFormat(f.Encoding.ToFrontend(),
                f.SampleRate, f.Channels, f.AverageBytesPerSecond, f.BlockAlign, f.BitsPerSample);
        }

        public static MqttQualityOfServiceLevel ToFrontend(this Common.MqttQualityOfServiceLevel qos)
        {
            return ToEnum<MqttQualityOfServiceLevel>(qos);
        }

        public static MqttApplicationMessage ToFrontend(this Common.MqttApplicationMessage message)
        {
            return ToFrontend(message, new MqttApplicationMessage());
        }

        public static MqttApplicationMessage ToFrontend(this Common.MqttApplicationMessage src, MqttApplicationMessage dest)
        {
            dest.Payload = src.Payload;
            dest.QualityOfServiceLevel = src.QualityOfServiceLevel.ToFrontend();
            dest.Retain = src.Retain;
            dest.Topic = src.Topic;

            return dest;
        }

        public static MqttApplicationMessageInterceptorContext ToFrontend(this Common.MqttApplicationMessageInterceptorContext src,
            MqttApplicationMessageInterceptorContext dest)
        {
            dest.AcceptPublish = src.AcceptPublish;
            src.ApplicationMessage.ToFrontend(dest.ApplicationMessage);

            return dest;
        }

        private static T ToEnum<T>(Enum value)
        {
            Type type = typeof(T);
            string name = Enum.GetName(value.GetType(), value);

            return (T)Enum.Parse(type, name);
        }
    }
}
