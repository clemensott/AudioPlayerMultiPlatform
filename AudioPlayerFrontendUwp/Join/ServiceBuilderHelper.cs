using System;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    class ServiceBuilderHelper : NotifyPropertyChangedHelper, IServiceBuilderHelper
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

        public Func<IAudioService> CreateAudioService => DoCreateAudioService;

        private ServiceBuilderHelper() { }

        private IAudioService DoCreateAudioService()
        {
            return new AudioService(AudioServiceHelper.Current);
        }

        public AudioServicePlayer CreateAudioServicePlayer(IWaveProviderPlayer player, IAudioService service)
        {
            return new AudioServicePlayer(service, player, PlayerHelper.Current);
        }

        public AudioStreamPlayer CreateAudioStreamPlayer(IWaveProviderPlayer player, IAudioService service)
        {
            return new AudioStreamPlayer(service, player, PlayerHelper.Current);
        }
    }
}
