using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;

namespace AudioPlayerFrontend.Join
{
    internal class PlayerCreateService : IPlayerCreateService
    {
        public AudioServicePlayer CreateAudioServicePlayer(IAudioService service)
        {
            return new AudioServicePlayer(service, new Player());
        }

        public AudioStreamPlayer CreateAudioStreamPlayer(IAudioService service)
        {
            return new AudioStreamPlayer(service, new Player());
        }
    }
}
