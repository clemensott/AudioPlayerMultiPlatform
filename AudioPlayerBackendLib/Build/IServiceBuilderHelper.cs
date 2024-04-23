using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;

namespace AudioPlayerBackend.Build
{
    public interface IServiceBuilderHelper
    {
        IInvokeDispatcherService Dispatcher { get; }

        AudioStreamPlayer CreateAudioStreamPlayer(IPlayer player, IAudioService service);

        AudioServicePlayer CreateAudioServicePlayer(IPlayer player, IAudioService service);
    }
}
