using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using System;

namespace AudioPlayerBackend
{
    public interface IServiceBuilderHelper : INotifyPropertyChangedHelper
    {
        Func<IAudioService> CreateAudioService { get; }

        AudioStreamPlayer CreateAudioStreamPlayer(IWaveProviderPlayer player, IAudioService service);

        AudioServicePlayer CreateAudioServicePlayer(IWaveProviderPlayer player, IAudioService service);
    }
}
