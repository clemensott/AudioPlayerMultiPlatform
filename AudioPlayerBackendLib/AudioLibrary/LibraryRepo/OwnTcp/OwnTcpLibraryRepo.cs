using AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.Player;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp
{
    internal class OwnTcpLibraryRepo : ILibraryRepo
    {
        private readonly IClientCommunicator clientCommunicator;

        public event EventHandler<AudioLibraryChangeArgs<PlaybackState>> OnPlayStateChange;
        public event EventHandler<AudioLibraryChangeArgs<double>> OnVolumeChange;
        public event EventHandler<AudioLibraryChangeArgs<Guid?>> OnCurrentPlaylistIdChange;
        public event EventHandler<AudioLibraryChangeArgs<DateTime?>> OnFoldersLastUpdatedChange;

        public OwnTcpLibraryRepo(IClientCommunicator clientCommunicator)
        {
            this.clientCommunicator = clientCommunicator;
        }

        public Task Start()
        {
            clientCommunicator.Received += ClientCommunicator_Received;
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            clientCommunicator.Received -= ClientCommunicator_Received;
            return Task.CompletedTask;
        }

        public Task Dispose()
        {
            return Stop();
        }

        private void ClientCommunicator_Received(object sender, ReceivedEventArgs e)
        {
            TaskCompletionSource<byte[]> anwser = null;
            try
            {
                string[] parts = e.Topic.Split('.');
                if (parts.Length != 2 || parts[0] != nameof(ILibraryRepo)) return;

                anwser = e.StartAnwser();

                ByteQueue payload = e.Payload;
                switch (parts[1])
                {
                    case nameof(OnPlayStateChange):
                        PlaybackState playState = payload.DequeuePlaybackState();
                        OnPlayStateChange?.Invoke(this, new AudioLibraryChangeArgs<PlaybackState>(playState));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnVolumeChange):
                        double volume = payload.DequeueDouble();
                        OnVolumeChange?.Invoke(this, new AudioLibraryChangeArgs<double>(volume));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnCurrentPlaylistIdChange):
                        Guid? currentPlaylistId = payload.DequeueGuidNullable();
                        OnCurrentPlaylistIdChange?.Invoke(this, new AudioLibraryChangeArgs<Guid?>(currentPlaylistId));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnFoldersLastUpdatedChange):
                        DateTime? foldersLastUpdated = payload.DequeueDateTimeNullable();
                        OnFoldersLastUpdatedChange?.Invoke(this, new AudioLibraryChangeArgs<DateTime?>(foldersLastUpdated));
                        anwser.SetResult(null);
                        break;

                    default:
                        anwser.SetException(new NotSupportedException($"Received action is not supported: {parts[2]}"));
                        break;
                }
            }
            catch (Exception exception)
            {
                if (anwser != null) anwser.SetException(exception);
                else throw;
            }
        }

        public Task<byte[]> SendAsync(string funcName, byte[] payload = null)
        {
            return clientCommunicator.SendAsync($"{nameof(ILibraryRepo)}.{funcName}", payload);
        }

        public async Task<Library> GetLibrary()
        {
            ByteQueue result = await SendAsync(nameof(GetLibrary));
            return result.DequeueLibrary();
        }

        public async Task SendPlayStateChange(PlaybackState playState)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(playState);
            await SendAsync(nameof(SendPlayStateChange), payload);

            OnPlayStateChange?.Invoke(this, new AudioLibraryChangeArgs<PlaybackState>(playState));
        }

        public async Task SendVolumeChange(double volume)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(volume);
            await SendAsync(nameof(SendVolumeChange), payload);

            OnVolumeChange?.Invoke(this, new AudioLibraryChangeArgs<double>(volume));

        }

        public async Task SendCurrentPlaylistIdChange(Guid? currentPlaylistId)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(currentPlaylistId);
            await SendAsync(nameof(SendCurrentPlaylistIdChange), payload);

            OnCurrentPlaylistIdChange?.Invoke(this, new AudioLibraryChangeArgs<Guid?>(currentPlaylistId));
        }

        public async Task SendFoldersLastUpdatedChange(DateTime? foldersLastUpdated)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(foldersLastUpdated);
            await SendAsync(nameof(SendFoldersLastUpdatedChange), payload);

            OnFoldersLastUpdatedChange?.Invoke(this, new AudioLibraryChangeArgs<DateTime?>(foldersLastUpdated));
        }
    }
}
