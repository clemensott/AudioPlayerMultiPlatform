using System.Net.Sockets;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    class OwnTcpServerConnection : OwnTcpConnection
    {
        public OwnTcpServerConnection(TcpClient client, OwnTcpSendQueue sendQueue) : base(client)
        {
        }
    }
}
