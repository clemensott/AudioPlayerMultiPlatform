using AudioPlayerBackend;
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

        public IInvokeDispatcherHelper Dispatcher { get; } = new InvokeDispatcherHelper();

        private ServiceBuilderHelper() { }

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
