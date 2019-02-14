using System;

namespace AudioPlayerBackend.Common
{
    public class MqttClientDisconnectedEventArgs : EventArgs
    {
        public bool ClientWasConnected { get; private set; }

        public Exception Exception { get; private set; }

        public MqttClientDisconnectedEventArgs(bool clientWasConnected, Exception exception)
        {
            ClientWasConnected = clientWasConnected;
            Exception = exception;
        }
    }
}
