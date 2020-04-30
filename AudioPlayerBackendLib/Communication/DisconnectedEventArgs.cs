using System;

namespace AudioPlayerBackend.Communication
{
    public class DisconnectedEventArgs : EventArgs
    {
        public bool OnDisconnect { get; }

        public Exception Exception { get; }

        public DisconnectedEventArgs(bool onDisconnect, Exception exception)
        {
            OnDisconnect = onDisconnect;
            Exception = exception;
        }
    }
}
