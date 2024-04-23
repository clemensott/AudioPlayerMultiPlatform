using AudioPlayerBackend.Audio;

namespace AudioPlayerBackend.Player
{
    public interface IPlayerCreateService
    {
        AudioServicePlayer CreateAudioServicePlayer(IAudioService service);

        AudioStreamPlayer CreateAudioStreamPlayer(IAudioService service);
    }
}
