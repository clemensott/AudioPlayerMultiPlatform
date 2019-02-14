using System;

namespace AudioPlayerBackend.Common
{
    public class MqttClientConnectedEventArgs : EventArgs
    {
        public bool IsSessionPresent { get; private set; }

        public MqttClientConnectedEventArgs(bool isSessionPresent)
        {
            IsSessionPresent = isSessionPresent;
        }
    }
}
