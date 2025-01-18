using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.OwnTcp;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.FileSystem.OwnTcp
{
    internal class OwnTcpServerUpdateLibraryServiceConnector : OwnTcpBaseServerConnector, IServerUpdateLibraryServiceConnector
    {
        private readonly IUpdateLibraryService updateLibraryService;

        public OwnTcpServerUpdateLibraryServiceConnector(IUpdateLibraryService updateLibraryService, IServerCommunicator serverCommunicator)
            : base(nameof(IUpdateLibraryService), serverCommunicator)
        {
            this.updateLibraryService = updateLibraryService;
        }

        protected override void SubscribeToService()
        {
            updateLibraryService.UpdateStarted += OnUpdateStarted;
            updateLibraryService.UpdateCompleted += OnUpdateCompleted;
        }

        protected override void UnsubscribeFromService()
        {
            updateLibraryService.UpdateStarted -= OnUpdateStarted;
            updateLibraryService.UpdateCompleted -= OnUpdateCompleted;
        }

        private async void OnUpdateStarted(object sender, EventArgs e)
        {
            await SendAsync(nameof(updateLibraryService.UpdateStarted));
        }

        private async void OnUpdateCompleted(object sender, EventArgs e)
        {
            await SendAsync(nameof(updateLibraryService.UpdateCompleted));
        }

        protected override async Task<byte[]> OnMessageReceived(string subTopic, byte[] payload)
        {
            ByteQueue queue = payload;
            switch (subTopic)
            {
                case nameof(updateLibraryService.ReloadLibrary):
                    await updateLibraryService.ReloadLibrary();
                    return null;

                case nameof(updateLibraryService.UpdateLibrary):
                    await updateLibraryService.UpdateLibrary();
                    return null;

                case nameof(updateLibraryService.UpdatePlaylists):
                    await updateLibraryService.UpdatePlaylists();
                    return null;

                case nameof(updateLibraryService.ReloadSourcePlaylist):
                    await updateLibraryService.ReloadSourcePlaylist(queue.DequeueGuid());
                    return null;

                case nameof(updateLibraryService.LoadSongs):
                    Song[] songs = await updateLibraryService.LoadSongs(queue.DequeueFileMediaSources());
                    return new ByteQueue()
                        .Enqueue(songs);

                case nameof(updateLibraryService.UpdateSourcePlaylist):
                    await updateLibraryService.UpdateSourcePlaylist(queue.DequeueGuid());
                    return null;

                default:
                    throw new NotSupportedException($"Received action is not supported: {subTopic}");
            }
        }
    }
}
