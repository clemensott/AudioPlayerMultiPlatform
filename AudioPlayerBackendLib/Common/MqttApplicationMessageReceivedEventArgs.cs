using System;

namespace AudioPlayerBackend.Common
{
    public class MqttApplicationMessageReceivedEventArgs : EventArgs
    {
        public MqttApplicationMessage ApplicationMessage { get; private set; }

        public MqttApplicationMessageReceivedEventArgs(MqttApplicationMessage applicationMessage)
        {
            ApplicationMessage = applicationMessage;
        }
    }
}