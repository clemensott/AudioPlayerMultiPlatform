using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;

namespace AudioPlayerBackend.Build
{
    public interface IServiceBuilderHelper
    {
        IInvokeDispatcherHelper Dispatcher { get; }

        AudioStreamPlayer CreateAudioStreamPlayer(IPlayer player, IAudioService service);

        AudioServicePlayer CreateAudioServicePlayer(IPlayer player, IAudioService service);
    }
}
