using System;

namespace AudioPlayerBackend.Audio
{
    internal class AudioCreateService : IAudioCreateService
    {
        public IAudioService CreateAudioService()
        {
            return new AudioService();
        }

        public IPlaylist CreatePlaylist(Guid id)
        {
            return new Playlist(id);
        }

        public ISourcePlaylist CreateSourcePlaylist(Guid id)
        {
            return new SourcePlaylist(id);
        }
    }
}
