using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Audio
{
    public interface IAudioCreateService
    {
        IAudioService CreateAudioService();

        IPlaylist CreatePlaylist(Guid id);

        ISourcePlaylist CreateSourcePlaylist(Guid id);
    }
}
