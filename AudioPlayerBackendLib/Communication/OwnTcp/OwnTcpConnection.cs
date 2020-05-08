using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    class OwnTcpConnection
    {
        public TcpClient Client { get; }

        public  NetworkStream Stream { get; }

        public Task Task { get; set; }

        public OwnTcpSendQueue SendQueue { get; }

        public OwnTcpConnection(TcpClient client, OwnTcpSendQueue sendQueue)
        {
            Client = client;
            Stream = client.GetStream();
            SendQueue = sendQueue;
        }
    }
}
