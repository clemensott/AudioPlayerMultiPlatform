using System;

namespace AudioPlayerBackend.GenericEventArgs
{
    public class HandledEventArgs : EventArgs
    {
        public bool Handled { get; set; }

        public HandledEventArgs()
        {
        }

        public HandledEventArgs(bool handled)
        {
            Handled = handled;
        }
    }
}
