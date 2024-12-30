using AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.OwnTcp;
using AudioPlayerBackend.Player;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp
{
    internal class OwnTcpLibraryRepo : OwnTcpBaseService, ILibraryRepo
    {
        public event EventHandler<AudioLibraryChangeArgs<PlaybackState>> PlayStateChanged;
        public event EventHandler<AudioLibraryChangeArgs<double>> VolumeChanged;
        public event EventHandler<AudioLibraryChangeArgs<Guid?>> CurrentPlaylistIdChanged;
        public event EventHandler<AudioLibraryChangeArgs<DateTime?>> FoldersLastUpdatedChanged;

        public OwnTcpLibraryRepo(IClientCommunicator clientCommunicator) : base(nameof(ILibraryRepo), clientCommunicator)
        {
        }

        protected override Task<byte[]> OnMessageReceived(string subTopic, byte[] payload)
        {
            return Task.FromResult(OnMessageReceived(subTopic, payload));
        }

        private byte[] OnMessageReceived(string subTopic, ByteQueue payload)
        {
            switch (subTopic)
            {
                case nameof(PlayStateChanged):
                    PlaybackState playState = payload.DequeuePlaybackState();
                    PlayStateChanged?.Invoke(this, new AudioLibraryChangeArgs<PlaybackState>(playState));
                    return null;

                case nameof(VolumeChanged):
                    double volume = payload.DequeueDouble();
                    VolumeChanged?.Invoke(this, new AudioLibraryChangeArgs<double>(volume));
                    return null;

                case nameof(CurrentPlaylistIdChanged):
                    Guid? currentPlaylistId = payload.DequeueGuidNullable();
                    CurrentPlaylistIdChanged?.Invoke(this, new AudioLibraryChangeArgs<Guid?>(currentPlaylistId));
                    return null;

                case nameof(FoldersLastUpdatedChanged):
                    DateTime? foldersLastUpdated = payload.DequeueDateTimeNullable();
                    FoldersLastUpdatedChanged?.Invoke(this, new AudioLibraryChangeArgs<DateTime?>(foldersLastUpdated));
                    return null;

                default:
                    throw new NotSupportedException($"Received action is not supported: {subTopic}");
            }
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
