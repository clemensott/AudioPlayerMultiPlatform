using StdOttStandard.AsyncResult;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    class OwnTcpSendMessage : AsyncResult<bool>
    {
        public OwnTcpMessage Message { get; set; }

        public OwnTcpSendMessage(OwnTcpMessage message)
        {
            Message = message;
        }
    }
}
