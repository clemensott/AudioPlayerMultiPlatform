using AudioPlayerBackend.Audio;

namespace AudioPlayerBackend.Player
{
    public interface IPlayerCreateService
    {
        IPlayerService CreateAudioServicePlayer(IAudioService service);

        IPlayerService CreateAudioStreamPlayer(IAudioService service);
    }
}
