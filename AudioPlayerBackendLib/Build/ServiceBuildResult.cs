using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Data;
using AudioPlayerBackend.Player;

namespace AudioPlayerBackend.Build
{
    public class ServiceBuildResult
    {
        public IAudioService AudioService { get; }

        public ICommunicator Communicator { get; }

        public IPlayerService ServicePlayer { get; }

        public ReadWriteAudioServiceData Data { get; }

        public ServiceBuildResult(IAudioService audioService, ICommunicator communicator,
            IPlayerService servicePlayer, ReadWriteAudioServiceData data)
        {
            AudioService = audioService;
            Communicator = communicator;
            ServicePlayer = servicePlayer;
            Data = data;
        }
    }
}
