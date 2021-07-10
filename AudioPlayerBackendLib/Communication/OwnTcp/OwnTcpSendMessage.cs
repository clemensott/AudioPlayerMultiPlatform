using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    class OwnTcpSendMessage : TaskCompletionSource<bool>
    {
        public OwnTcpMessage Message { get; set; }

        public OwnTcpSendMessage(OwnTcpMessage message)
        {
            Message = message;
        }
    }
}
