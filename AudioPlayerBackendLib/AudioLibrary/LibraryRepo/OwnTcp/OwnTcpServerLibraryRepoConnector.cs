using AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.OwnTcp;
using AudioPlayerBackend.Player;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp
{
    internal class OwnTcpServerLibraryRepoConnector : OwnTcpBaseServerConnector, IServerLibraryRepoConnector
    {
        private readonly ILibraryRepo libraryRepo;

        public OwnTcpServerLibraryRepoConnector(ILibraryRepo libraryRepo, IServerCommunicator serverCommunicator)
            : base(nameof(ILibraryRepo), serverCommunicator)
        {
            this.libraryRepo = libraryRepo;
        }

        protected override void SubscribeToService()
        {
            libraryRepo.PlayStateChanged += OnPlayStateChanged;
            libraryRepo.VolumeChanged += OnVolumeChanged;
            libraryRepo.CurrentPlaylistIdChanged += OnCurrentPlaylistIdChanged;
            libraryRepo.FoldersLastUpdatedChanged += OnFoldersLastUpdatedChanged;
        }

        protected override void UnsubscribeFromService()
        {
            libraryRepo.PlayStateChanged -= OnPlayStateChanged;
            libraryRepo.VolumeChanged -= OnVolumeChanged;
            libraryRepo.CurrentPlaylistIdChanged -= OnCurrentPlaylistIdChanged;
        }

        private async void OnPlayStateChanged(object sender, AudioLibraryChangeArgs<PlaybackState> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.NewValue);
            await SendAsync(nameof(libraryRepo.PlayStateChanged), payload);
        }

        private async void OnVolumeChanged(object sender, AudioLibraryChangeArgs<double> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.NewValue);
            await SendAsync(nameof(libraryRepo.VolumeChanged), payload);
        }

        private async void OnCurrentPlaylistIdChanged(object sender, AudioLibraryChangeArgs<Guid?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.NewValue);
            await SendAsync(nameof(libraryRepo.CurrentPlaylistIdChanged), payload);
        }

        private async void OnFoldersLastUpdatedChanged(object sender, AudioLibraryChangeArgs<DateTime?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.NewValue);
            await SendAsync(nameof(libraryRepo.FoldersLastUpdatedChanged), payload);
        }

        protected override async Task<byte[]> OnMessageReceived(string subTopic, byte[] payload)
        {
            ByteQueue queue = payload;
            switch (subTopic)
            {
                case nameof(libraryRepo.GetLibrary):
                    Library library = await libraryRepo.GetLibrary();
                    return new ByteQueue()
                        .Enqueue(library);

                case nameof(libraryRepo.SetPlayState):
                    PlaybackState playState = queue.DequeuePlaybackState();
                    await libraryRepo.SetPlayState(playState);
                    return null;

                case nameof(libraryRepo.SetVolume):
                    double volume = queue.DequeueDouble();
                    await libraryRepo.SetVolume(volume); 
                    return null;

                case nameof(libraryRepo.SetCurrentPlaylistId):
                    Guid? currentPlaylistId = queue.DequeueGuidNullable();
                    await libraryRepo.SetCurrentPlaylistId(currentPlaylistId);
                    return null;

                case nameof(libraryRepo.SetFoldersLastUpdated):
                    DateTime? foldersLastUpdated = queue.DequeueDateTimeNullable();
                    await libraryRepo.SetFoldersLastUpdated(foldersLastUpdated);
                    return null;

                default:
                    throw new NotSupportedException($"Received action is not supported: {subTopic}");
            }
        }
    }
}
