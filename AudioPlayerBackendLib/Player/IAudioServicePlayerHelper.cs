using AudioPlayerBackend.Audio;
using System;

namespace AudioPlayerBackend.Player
{
    public interface IAudioServicePlayerHelper : INotifyPropertyChangedHelper
    {
        IPositionWaveProvider CreateWaveProvider(Song song, IAudioService service);

        Action<IServicePlayer> SetCurrentSongThreadSafe { get; }
    }
}
