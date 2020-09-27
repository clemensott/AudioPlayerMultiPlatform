using AudioPlayerBackend.Audio;
using System;

namespace AudioPlayerBackend.Player
{
    public interface IAudioServicePlayerHelper
    {
        IPositionWaveProvider CreateWaveProvider(Song song, IAudioService service);

        Action<IServicePlayer> SetWannaSongThreadSafe { get; }

        void Reload(ISourcePlaylistBase playlist);

        void Update(ISourcePlaylistBase playlist);
    }
}
