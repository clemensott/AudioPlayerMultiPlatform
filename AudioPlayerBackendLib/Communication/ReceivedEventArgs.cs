using System;

namespace AudioPlayerBackend.Communication
{
    public class ReceivedEventArgs: EventArgs
    {
        public string Topic { get; }

        public byte[] Payload { get; }

        public ReceivedEventArgs(string topic, byte[] payload)
        {
            Topic = topic;
            Payload = payload;
        }
    }
}
