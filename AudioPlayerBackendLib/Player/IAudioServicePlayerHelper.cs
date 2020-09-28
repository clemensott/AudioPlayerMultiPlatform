using AudioPlayerBackend.Audio;
using System;

namespace AudioPlayerBackend.Player
{
    public interface IAudioServicePlayerHelper
    {
        void Reload(ISourcePlaylistBase playlist);

        void Update(ISourcePlaylistBase playlist);
    }
}
