using System;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    class ServiceBuilderHelper : IServiceBuilderHelper
    {
        private static ServiceBuilderHelper instance;

        public static ServiceBuilderHelper Current
        {
            get
            {
                if (instance == null) instance = new ServiceBuilderHelper();

                return instance;
            }
        }

        private ServiceBuilderHelper() { }

        public Action<Action> InvokeDispatcher => null;

        public AudioServicePlayer CreateAudioServicePlayer(IPlayer player, IAudioService service)
        {
            return new AudioServicePlayer(service, player);
        }

        public AudioStreamPlayer CreateAudioStreamPlayer(IPlayer player, IAudioService service)
        {
            return new AudioStreamPlayer(service, player, PlayerHelper.Current);
        }
    }
}
