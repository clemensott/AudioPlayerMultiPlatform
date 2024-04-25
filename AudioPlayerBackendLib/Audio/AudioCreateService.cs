using System;

namespace AudioPlayerBackend.Audio
{
    internal class AudioCreateService : IAudioCreateService
    {
        private readonly IInvokeDispatcherService dispatcher;

        public AudioCreateService(IInvokeDispatcherService dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public IAudioService CreateAudioService()
        {
            return new AudioService(dispatcher);
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
