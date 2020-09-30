using System;

namespace AudioPlayerFrontend.Join
{
    class HandledEventArgs : EventArgs
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
