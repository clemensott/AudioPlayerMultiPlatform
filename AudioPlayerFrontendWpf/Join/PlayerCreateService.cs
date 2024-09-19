using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    internal class PlayerCreateService : IPlayerCreateService
    {
        public IPlayerService CreateAudioServicePlayer(AudioPlayerBackend.IAudioService service)
        {
            throw new System.NotImplementedException();
        }

        public IPlayerService CreateAudioStreamPlayer(AudioPlayerBackend.IAudioService service)
        {
            throw new System.NotImplementedException();
        }
    }
}
