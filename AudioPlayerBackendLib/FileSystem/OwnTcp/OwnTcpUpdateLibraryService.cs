using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.OwnTcp;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.FileSystem.OwnTcp
{
    internal class OwnTcpUpdateLibraryService : OwnTcpBaseService, IUpdateLibraryService
    {
        public event EventHandler UpdateStarted;
        public event EventHandler UpdateCompleted;

        public OwnTcpUpdateLibraryService(IClientCommunicator clientCommunicator)
            : base(nameof(IUpdateLibraryService), clientCommunicator)
        {
        }

        protected override Task<byte[]> OnMessageReceived(string subTopic, byte[] payload)
        {
            return Task.FromResult(OnMessageReceived(subTopic, payload));
        }

        protected byte[] OnMessageReceived(string subTopic, ByteQueue payload)
        {
            switch (subTopic)
            {
                case nameof(UpdateStarted):
                    UpdateStarted?.Invoke(this, EventArgs.Empty);
                    return null;

                case nameof(UpdateCompleted):
                    UpdateCompleted?.Invoke(this, EventArgs.Empty);
                    return null;

                default:
                    throw new NotSupportedException($"Received action is not supported: {subTopic}");
            }
        }

        public async Task ReloadLibrary()
        {
            await SendAsync(nameof(ReloadLibrary));
        }

        public async Task UpdateLibrary()
        {
            await SendAsync(nameof(UpdateLibrary));
        }

        public async Task UpdatePlaylists()
        {
            await SendAsync(nameof(UpdatePlaylists));
        }

        public async Task ReloadSourcePlaylist(Guid id)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id);
            await SendAsync(nameof(ReloadSourcePlaylist), payload);
        }

        public async Task<Song[]> LoadSongs(FileMediaSources fileMediaSources)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(fileMediaSources);
            ByteQueue result = await SendAsync(nameof(LoadSongs), payload);

            return result.DequeueSongs();
        }

        public async Task UpdateSourcePlaylist(Guid id)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id);
            await SendAsync(nameof(UpdateSourcePlaylist), payload);
        }
    }
}
