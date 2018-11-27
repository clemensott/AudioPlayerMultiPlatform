namespace AudioPlayerBackend.Common
{
    public class MqttApplicationMessageInterceptorContext
    {
        public string ClientId { get; private set; }

        public MqttApplicationMessage ApplicationMessage { get; private set; }

        public bool AcceptPublish { get; set; }

        public MqttApplicationMessageInterceptorContext(string clientId, MqttApplicationMessage applicationMessage, bool acceptPublish)
        {
            ClientId = clientId;
            ApplicationMessage = applicationMessage;
            AcceptPublish = acceptPublish;
        }
    }
}