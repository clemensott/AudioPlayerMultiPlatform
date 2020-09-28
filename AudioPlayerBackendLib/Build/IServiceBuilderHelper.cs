using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using System;

namespace AudioPlayerBackend.Build
{
    public interface IServiceBuilderHelper : INotifyPropertyChangedHelper
    {
        AudioStreamPlayer CreateAudioStreamPlayer(IPlayer player, IAudioService service);

        AudioServicePlayer CreateAudioServicePlayer(IPlayer player, IAudioService service);
    }
}
