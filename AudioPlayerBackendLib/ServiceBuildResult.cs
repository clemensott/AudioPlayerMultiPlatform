using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Player;

namespace AudioPlayerBackend
{
    public class ServiceBuildResult
    {
        public IAudioService AudioService { get; }

        public ICommunicator Communicator { get; }

        public IServicePlayer ServicePlayer { get; }

        public ServiceBuildResult(IAudioService audioService, ICommunicator communicator, IServicePlayer servicePlayer)
        {
            AudioService = audioService;
            Communicator = communicator;
            ServicePlayer = servicePlayer;
        }
    }
}
