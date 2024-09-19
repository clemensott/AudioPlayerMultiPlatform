using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication
{
    public class ReceivedEventArgs: EventArgs
    {
        public string Topic { get; }

        public byte[] Payload { get; }

        public TaskCompletionSource<byte[]> Anwser { get; }

        public ReceivedEventArgs(string topic, byte[] payload)
        {
            Topic = topic;
            Payload = payload;
        }
    }
}
