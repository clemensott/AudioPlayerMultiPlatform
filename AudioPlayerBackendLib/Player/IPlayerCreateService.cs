using AudioPlayerBackend.Audio;

namespace AudioPlayerBackend.Player
{
    public interface IPlayerCreateService
    {
        IServicePlayer CreateAudioServicePlayer(IAudioService service);

        IServicePlayer CreateAudioStreamPlayer(IAudioService service);
    }
}
