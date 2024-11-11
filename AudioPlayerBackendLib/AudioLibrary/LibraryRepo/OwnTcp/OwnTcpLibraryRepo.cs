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

        public event EventHandler<AudioLibraryChangeArgs<PlaybackState>> PlayStateChanged;
        public event EventHandler<AudioLibraryChangeArgs<double>> VolumeChanged;
        public event EventHandler<AudioLibraryChangeArgs<Guid?>> CurrentPlaylistIdChanged;
        public event EventHandler<AudioLibraryChangeArgs<DateTime?>> FoldersLastUpdatedChanged;

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
                    case nameof(PlayStateChanged):
                        PlaybackState playState = payload.DequeuePlaybackState();
                        PlayStateChanged?.Invoke(this, new AudioLibraryChangeArgs<PlaybackState>(playState));
                        anwser.SetResult(null);
                        break;

                    case nameof(VolumeChanged):
                        double volume = payload.DequeueDouble();
                        VolumeChanged?.Invoke(this, new AudioLibraryChangeArgs<double>(volume));
                        anwser.SetResult(null);
                        break;

                    case nameof(CurrentPlaylistIdChanged):
                        Guid? currentPlaylistId = payload.DequeueGuidNullable();
                        CurrentPlaylistIdChanged?.Invoke(this, new AudioLibraryChangeArgs<Guid?>(currentPlaylistId));
                        anwser.SetResult(null);
                        break;

                    case nameof(FoldersLastUpdatedChanged):
                        DateTime? foldersLastUpdated = payload.DequeueDateTimeNullable();
                        FoldersLastUpdatedChanged?.Invoke(this, new AudioLibraryChangeArgs<DateTime?>(foldersLastUpdated));
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

        public async Task SetPlayState(PlaybackState playState)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(playState);
            await SendAsync(nameof(SetPlayState), payload);

            PlayStateChanged?.Invoke(this, new AudioLibraryChangeArgs<PlaybackState>(playState));
        }

        public async Task SetVolume(double volume)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(volume);
            await SendAsync(nameof(SetVolume), payload);

            VolumeChanged?.Invoke(this, new AudioLibraryChangeArgs<double>(volume));

        }

        public async Task SetCurrentPlaylistId(Guid? currentPlaylistId)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(currentPlaylistId);
            await SendAsync(nameof(SetCurrentPlaylistId), payload);

            CurrentPlaylistIdChanged?.Invoke(this, new AudioLibraryChangeArgs<Guid?>(currentPlaylistId));
        }

        public async Task SetFoldersLastUpdated(DateTime? foldersLastUpdated)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(foldersLastUpdated);
            await SendAsync(nameof(SetFoldersLastUpdated), payload);

            FoldersLastUpdatedChanged?.Invoke(this, new AudioLibraryChangeArgs<DateTime?>(foldersLastUpdated));
        }
    }
}
