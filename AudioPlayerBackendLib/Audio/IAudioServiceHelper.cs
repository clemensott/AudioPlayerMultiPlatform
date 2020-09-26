using System;

namespace AudioPlayerBackend.Audio
{
    public interface IAudioServiceHelper
    {
        void Reload(ISourcePlaylist playlist);
    }
}
