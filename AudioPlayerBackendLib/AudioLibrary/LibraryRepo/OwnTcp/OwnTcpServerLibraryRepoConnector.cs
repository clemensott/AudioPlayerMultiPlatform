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
            libraryRepo.OnPlayStateChange += OnPlayStateChange;
            libraryRepo.OnVolumeChange += OnVolumeChange;
            libraryRepo.OnCurrentPlaylistIdChange += OnCurrentPlaylistIdChange;
            libraryRepo.OnFoldersLastUpdatedChange += OnFoldersLastUpdatedChange;

            serverCommunicator.Received += OnReceived;

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            libraryRepo.OnPlayStateChange -= OnPlayStateChange;
            libraryRepo.OnVolumeChange -= OnVolumeChange;
            libraryRepo.OnCurrentPlaylistIdChange -= OnCurrentPlaylistIdChange;
            libraryRepo.OnFoldersLastUpdatedChange -= OnFoldersLastUpdatedChange;

            serverCommunicator.Received += OnReceived;

            return Task.CompletedTask;
        }

        private async void OnPlayStateChange(object sender, AudioLibraryChangeArgs<PlaybackState> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.NewValue);
            await SendAsync(nameof(libraryRepo.OnPlayStateChange), payload);
        }

        private async void OnVolumeChange(object sender, AudioLibraryChangeArgs<double> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.NewValue);
            await SendAsync(nameof(libraryRepo.OnVolumeChange), payload);
        }

        private async void OnCurrentPlaylistIdChange(object sender, AudioLibraryChangeArgs<Guid?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.NewValue);
            await SendAsync(nameof(libraryRepo.OnCurrentPlaylistIdChange), payload);
        }

        private async void OnFoldersLastUpdatedChange(object sender, AudioLibraryChangeArgs<DateTime?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.NewValue);
            await SendAsync(nameof(libraryRepo.OnFoldersLastUpdatedChange), payload);
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

                    case nameof(libraryRepo.SendPlayStateChange):
                        PlaybackState playState = payload.DequeuePlaybackState();
                        await libraryRepo.SendPlayStateChange(playState);
                        anwser.SetResult(null);
                        break;

                    case nameof(libraryRepo.SendVolumeChange):
                        double volume = payload.DequeueDouble();
                        await libraryRepo.SendVolumeChange(volume);
                        anwser.SetResult(null);
                        break;

                    case nameof(libraryRepo.SendCurrentPlaylistIdChange):
                        Guid? currentPlaylistId = payload.DequeueGuidNullable();
                        await libraryRepo.SendCurrentPlaylistIdChange(currentPlaylistId);
                        anwser.SetResult(null);
                        break;

                    case nameof(libraryRepo.SendFoldersLastUpdatedChange):
                        DateTime? foldersLastUpdated = payload.DequeueDateTimeNullable();
                        await libraryRepo.SendFoldersLastUpdatedChange(foldersLastUpdated);
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
