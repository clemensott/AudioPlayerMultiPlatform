using AudioPlayerBackend.Player;
using System;

namespace AudioPlayerFrontend.Join
{
    class PlayerHelper : IAudioStreamHelper
    {
        private static PlayerHelper instance;

        public static PlayerHelper Current
        {
            get
            {
                if (instance == null) instance = new PlayerHelper();

                return instance;
            }
        }

        public Action<Action> InvokeDispatcher => null;

        private PlayerHelper() { }
    }
}
