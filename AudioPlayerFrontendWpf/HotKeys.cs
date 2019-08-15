using StdOttFramework.Hotkey;
using System;

namespace AudioPlayerFrontend
{
    public class HotKeys : IDisposable
    {
        private HotKey toggle, next, previous, play, pause, restart;

        public HotKey Toggle
        {
            get => toggle;
            set
            {
                if (value == toggle) return;

                if (toggle != null) toggle.Pressed -= Toggle_Press;

                toggle = value;

                if (toggle != null) toggle.Pressed += Toggle_Press;
            }
        }

        public HotKey Next
        {
            get => next;
            set
            {
                if (value == next) return;

                if (next != null) next.Pressed -= Next_Press;

                next = value;

                if (next != null) next.Pressed += Next_Press;
            }
        }

        public HotKey Previous
        {
            get => previous;
            set
            {
                if (value == previous) return;

                if (previous != null) previous.Pressed -= Previous_Press;

                previous = value;

                if (previous != null) previous.Pressed += Previous_Press;
            }
        }

        public HotKey Play
        {
            get => play;
            set
            {
                if (value == play) return;

                if (play != null) play.Pressed -= Play_Press;

                play = value;

                if (play != null) play.Pressed += Play_Press;
            }
        }

        public HotKey Pause
        {
            get => pause;
            set
            {
                if (value == pause) return;

                if (pause != null) pause.Pressed -= Pause_Press;

                pause = value;

                if (pause != null) pause.Pressed += Pause_Press;
            }
        }

        public HotKey Restart
        {
            get => restart;
            set
            {
                if (value == restart) return;

                if (restart != null) restart.Pressed -= Restart_Press;

                restart = value;

                if (restart != null) restart.Pressed += Restart_Press;
            }
        }

        public event EventHandler<EventArgs> Toggle_Pressed, Next_Pressed, Previous_Pressed,
            Play_Pressed, Pause_Pressed, Restart_Pressed;

        public HotKeys()
        {
        }

        private void Toggle_Press(object sender, KeyPressedEventArgs e)
        {
            Toggle_Pressed?.Invoke(this, EventArgs.Empty);
        }

        private void Next_Press(object sender, KeyPressedEventArgs e)
        {
            Next_Pressed?.Invoke(this, EventArgs.Empty);
        }

        private void Previous_Press(object sender, KeyPressedEventArgs e)
        {
            Previous_Pressed?.Invoke(this, EventArgs.Empty);
        }

        private void Play_Press(object sender, KeyPressedEventArgs e)
        {
            Play_Pressed?.Invoke(this, EventArgs.Empty);
        }

        private void Pause_Press(object sender, KeyPressedEventArgs e)
        {
            Pause_Pressed?.Invoke(this, EventArgs.Empty);
        }

        private void Restart_Press(object sender, KeyPressedEventArgs e)
        {
            Restart_Pressed?.Invoke(this, EventArgs.Empty);
        }

        public void Register()
        {
            Toggle?.Register();
            Next?.Register();
            Previous?.Register();
            Play?.Register();
            Pause?.Register();
            Restart?.Register();
        }

        public void Unregister()
        {
            Toggle?.Unregister();
            Next?.Unregister();
            Previous?.Unregister();
            Play?.Unregister();
            Pause?.Unregister();
            Restart?.Unregister();
        }

        public void Dispose()
        {
            Toggle?.Dispose();
            Next?.Dispose();
            Previous?.Dispose();
            Play?.Dispose();
            Pause?.Dispose();
            Restart?.Dispose();
        }
    }
}
