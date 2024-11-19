using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication
{
    public class ReceivedEventArgs : EventArgs
    {
        public string Topic { get; }

        public byte[] Payload { get; }

        public bool IsAwserStarted => Anwser != null;

        public TaskCompletionSource<byte[]> Anwser { get; private set; }

        public ReceivedEventArgs(string topic, byte[] payload)
        {
            Topic = topic;
            Payload = payload;
        }

        public TaskCompletionSource<byte[]> StartAnwser()
        {
            if (IsAwserStarted) throw new InvalidOperationException("Anwser already started");
            return Anwser = new TaskCompletionSource<byte[]>();
        }
    }
}
