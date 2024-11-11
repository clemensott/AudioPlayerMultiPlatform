using AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.Player;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp
{
    internal class OwnTcpServerLibraryRepoConnector : IServerLibraryRepoConnector
    {
        private readonly ILibraryRepo libraryRepo;
        private readonly IServerCommunicator serverCommunicator;

        public OwnTcpServerLibraryRepoConnector(ILibraryRepo libraryRepo, IServerCommunicator serverCommunicator)
        {
            this.libraryRepo = libraryRepo;
            this.serverCommunicator = serverCommunicator;
        }

        public Task<byte[]> SendAsync(string funcName, byte[] payload = null)
        {
            return serverCommunicator.SendAsync($"{nameof(ILibraryRepo)}.{funcName}", payload);
        }

        public Task Start()
        {
            libraryRepo.PlayStateChanged += OnPlayStateChanged;
            libraryRepo.VolumeChanged += OnVolumeChanged;
            libraryRepo.CurrentPlaylistIdChanged += OnCurrentPlaylistIdChanged;
            libraryRepo.FoldersLastUpdatedChanged += OnFoldersLastUpdatedChanged;

            serverCommunicator.Received += OnReceived;

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            libraryRepo.PlayStateChanged -= OnPlayStateChanged;
            libraryRepo.VolumeChanged -= OnVolumeChanged;
            libraryRepo.CurrentPlaylistIdChanged -= OnCurrentPlaylistIdChanged;
            libraryRepo.FoldersLastUpdatedChanged -= OnFoldersLastUpdatedChanged;

            serverCommunicator.Received += OnReceived;

            return Task.CompletedTask;
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

        private async void OnReceived(object sender, ReceivedEventArgs e)
        {
            TaskCompletionSource<byte[]> anwser = null;
            try
            {
                string[] parts = e.Topic.Split('.');
                if (parts.Length != 2 || parts[0] != nameof(ILibraryRepo)) return;

                anwser = e.StartAnwser();

                ByteQueue payload = e.Payload;
                ByteQueue result;
                switch (parts[1])
                {
                    case nameof(libraryRepo.GetLibrary):
                        Library library = await libraryRepo.GetLibrary();
                        result = new ByteQueue()
                            .Enqueue(library);
                        anwser.SetResult(result);
                        break;

                    case nameof(libraryRepo.SetPlayState):
                        PlaybackState playState = payload.DequeuePlaybackState();
                        await libraryRepo.SetPlayState(playState);
                        anwser.SetResult(null);
                        break;

                    case nameof(libraryRepo.SetVolume):
                        double volume = payload.DequeueDouble();
                        await libraryRepo.SetVolume(volume);
                        anwser.SetResult(null);
                        break;

                    case nameof(libraryRepo.SetCurrentPlaylistId):
                        Guid? currentPlaylistId = payload.DequeueGuidNullable();
                        await libraryRepo.SetCurrentPlaylistId(currentPlaylistId);
                        anwser.SetResult(null);
                        break;

                    case nameof(libraryRepo.SetFoldersLastUpdated):
                        DateTime? foldersLastUpdated = payload.DequeueDateTimeNullable();
                        await libraryRepo.SetFoldersLastUpdated(foldersLastUpdated);
                        anwser.SetResult(null);
                        break;

                    default:
                        anwser.SetException(new NotSupportedException($"Received action is not supported: {parts[1]}"));
                        break;
                }
            }
            catch (Exception exception)
            {
                if (anwser != null) anwser.SetException(exception);
                else throw;
            }
        }

        public async Task Dispose()
        {
            await Stop();
        }
    }
}
