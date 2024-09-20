using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication.Base;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    public abstract class OwnTcpCommunicator : BaseCommunicator
    {
        public const string AnwserCmd = "-ans", ReturnCmd = "-rtn", PingCmd = "-ping", CloseCmd = "-close";

        protected readonly IAudioCreateService audioCreateService;

        protected OwnTcpCommunicator()
        {
            audioCreateService = AudioPlayerServiceProvider.Current.GetAudioCreateService();
        }

        private Task SendAsync(IPlaylistBase playlist, string topic, byte[] payload, bool fireAndForget)
        {
            return SendAsync(playlist.ID + "." + topic, payload, fireAndForget);
        }

        protected abstract Task<byte[]> SendAsync(string topic, byte[] payload, bool fireAndForget);

        protected static IEnumerable<byte> GetBytes(OwnTcpMessage message)
        {
            byte[] idBytes = BitConverter.GetBytes(message.ID);
            byte[] fireAndForgetBytes = BitConverter.GetBytes(message.IsFireAndForget);
            byte[] topicBytes = Encoding.UTF8.GetBytes(message.Topic);
            byte[] topicLengthBytes = BitConverter.GetBytes(topicBytes.Length);
            byte[] payloadLengthBytes = BitConverter.GetBytes(message.Payload?.Length ?? -1);

            return idBytes.Concat(fireAndForgetBytes)
                .Concat(topicLengthBytes)
                .Concat(topicBytes)
                .Concat(payloadLengthBytes)
                .Concat(message.Payload ?? new byte[0]);
        }
    }
}
