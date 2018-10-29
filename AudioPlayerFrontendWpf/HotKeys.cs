using StdOttWpfLib.Hotkey;
using System;

namespace AudioPlayerFrontendWpf
{
    public class HotKeys : IDisposable
    {

        private HotKey toggle, next, previous, play, pause, restart;

        public HotKey Toggle
        {
            get { return toggle; }
            set
            {
                if (value == toggle) return;

                if (toggle != null)
                {
                    toggle.Pressed -= Toggle_Press;
                    toggle.Unregister();
                }

                toggle = value;

                if (toggle != null)
                {
                    toggle.Pressed += Toggle_Press;
                    toggle.Register();
                }
            }
        }

        public HotKey Next
        {
            get { return next; }
            set
            {
                if (value == next) return;

                if (next != null)
                {
                    next.Pressed -= Toggle_Press;
                    next.Unregister();
                }

                next = value;

                if (next != null)
                {
                    next.Pressed += Toggle_Press;
                    next.Register();
                }
            }
        }

        public HotKey Previous
        {
            get { return previous; }
            set
            {
                if (value == previous) return;

                if (previous != null)
                {
                    previous.Pressed -= Toggle_Press;
                    previous.Unregister();
                }

                previous = value;

                if (previous != null)
                {
                    previous.Pressed += Toggle_Press;
                    previous.Register();
                }
            }
        }

        public HotKey Play
        {
            get { return play; }
            set
            {
                if (value == play) return;

                if (play != null)
                {
                    play.Pressed -= Toggle_Press;
                    play.Unregister();
                }

                play = value;

                if (play != null)
                {
                    play.Pressed += Toggle_Press;
                    play.Register();
                }
            }
        }

        public HotKey Pause
        {
            get { return pause; }
            set
            {
                if (value == pause) return;

                if (pause != null)
                {
                    pause.Pressed -= Toggle_Press;
                    pause.Unregister();
                }

                pause = value;

                if (pause != null)
                {
                    pause.Pressed += Toggle_Press;
                    pause.Register();
                }
            }
        }

        public HotKey Restart
        {
            get { return restart; }
            set
            {
                if (value == restart) return;

                if (restart != null)
                {
                    restart.Pressed -= Toggle_Press;
                    restart.Unregister();
                }

                restart = value;

                if (restart != null)
                {
                    restart.Pressed += Toggle_Press;
                    restart.Register();
                }
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
