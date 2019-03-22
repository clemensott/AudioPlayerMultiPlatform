using System;

namespace AudioPlayerBackend.Audio
{
    public interface IAudioServiceHelper : INotifyPropertyChangedHelper
    {
        void Reload(ISourcePlaylist playlist);
    }
}
