using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    class OwnTcpConnection
    {
        public TcpClient Client { get; }

        public NetworkStream Stream { get; }

        public OwnTcpSendQueue SendQueue { get; }

        public Task Task { get; set; }

        public OwnTcpConnection(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
            SendQueue = new OwnTcpSendQueue();
        }

        public async Task<OwnTcpMessage> ReadMessage()
        {
            byte[] idBytes = await ReadAsync(sizeof(uint));
            if (idBytes == null) return null;

            uint id = BitConverter.ToUInt32(idBytes, 0);
            bool fireAndForget = BitConverter.ToBoolean(await ReadAsync(sizeof(bool)), 0);
            int topicLength = BitConverter.ToInt32(await ReadAsync(sizeof(int)), 0);
            string topic = Encoding.UTF8.GetString(await ReadAsync(topicLength));
            int payloadLength = BitConverter.ToInt32(await ReadAsync(sizeof(int)), 0);

            byte[] payload;
            if (payloadLength > 0) payload = await ReadAsync(payloadLength);
            else if (payloadLength == 0) payload = new byte[0];
            else payload = null;

            return new OwnTcpMessage()
            {
                IsFireAndForget = fireAndForget,
                ID = id,
                Topic = topic,
                Payload = payload,
            };
        }

        private async Task<byte[]> ReadAsync(int count)
        {
            byte[] buffer = new byte[count];
            int remainingCount = count;
            do
            {
                int readCount = await Stream.ReadAsync(buffer, count - remainingCount, remainingCount);
                if (readCount == 0) return null;

                remainingCount -= readCount;
            } while (remainingCount > 0);

            return buffer;
        }
    }
}
