using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.OwnTcp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp
{
    internal class OwnTcpServerPlaylistsRepoConnector : OwnTcpBaseServerConnector, IServerPlaylistsRepoConnector
    {
        private readonly IPlaylistsRepo playlistsRepo;

        public OwnTcpServerPlaylistsRepoConnector(IPlaylistsRepo playlistsRepo, IServerCommunicator serverCommunicator)
            : base(nameof(IPlaylistsRepo), serverCommunicator)
        {
            this.playlistsRepo = playlistsRepo;
        }

        protected override void SubscribeToService()
        {
            playlistsRepo.InsertedPlaylist += OnInsertedPlaylist;
            playlistsRepo.RemovedPlaylist += OnRemovedPlaylist;
            playlistsRepo.NameChanged += OnNameChanged;
            playlistsRepo.ShuffleChanged += OnShuffleChanged;
            playlistsRepo.LoopChanged += OnLoopChanged;
            playlistsRepo.PlaybackRateChanged += OnPlaybackRateChanged;
            playlistsRepo.CurrentSongRequestChanged += OnCurrentSongRequestChanged;
            playlistsRepo.SongsChanged += OnSongsChanged;
            playlistsRepo.FileMedisSourcesChanged += OnFileMedisSourcesChanged;
            playlistsRepo.FilesLastUpdatedChanged += OnFilesLastUpdatedChanged;
            playlistsRepo.SongsLastUpdatedChanged += OnSongsLastUpdatedChanged;
        }

        protected override void UnsubscribeFromService()
        {
            playlistsRepo.InsertedPlaylist -= OnInsertedPlaylist;
            playlistsRepo.RemovedPlaylist -= OnRemovedPlaylist;
            playlistsRepo.NameChanged -= OnNameChanged;
            playlistsRepo.ShuffleChanged -= OnShuffleChanged;
            playlistsRepo.LoopChanged -= OnLoopChanged;
            playlistsRepo.PlaybackRateChanged -= OnPlaybackRateChanged;
            playlistsRepo.CurrentSongRequestChanged -= OnCurrentSongRequestChanged;
            playlistsRepo.SongsChanged -= OnSongsChanged;
            playlistsRepo.FileMedisSourcesChanged -= OnFileMedisSourcesChanged;
            playlistsRepo.FilesLastUpdatedChanged -= OnFilesLastUpdatedChanged;
            playlistsRepo.SongsLastUpdatedChanged -= OnSongsLastUpdatedChanged;
        }

        private async void OnInsertedPlaylist(object sender, InsertPlaylistArgs e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Index)
                .Enqueue(e.Playlist);
            await SendAsync(nameof(playlistsRepo.InsertedPlaylist), payload);
        }

        private async void OnRemovedPlaylist(object sender, RemovePlaylistArgs e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id);
            await SendAsync(nameof(playlistsRepo.RemovedPlaylist), payload);
        }

        private async void OnNameChanged(object sender, PlaylistChangeArgs<string> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.NameChanged), payload);
        }

        private async void OnShuffleChanged(object sender, PlaylistChangeArgs<OrderType> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.ShuffleChanged), payload);
        }

        private async void OnLoopChanged(object sender, PlaylistChangeArgs<LoopType> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.LoopChanged), payload);
        }

        private async void OnPlaybackRateChanged(object sender, PlaylistChangeArgs<double> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.PlaybackRateChanged), payload);
        }

        private async void OnCurrentSongRequestChanged(object sender, PlaylistChangeArgs<SongRequest?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.CurrentSongRequestChanged), payload);
        }

        private async void OnSongsChanged(object sender, PlaylistChangeArgs<ICollection<Song>> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.SongsChanged), payload);
        }

        private async void OnFileMedisSourcesChanged(object sender, PlaylistChangeArgs<MediaSource.FileMediaSources> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.FileMedisSourcesChanged), payload);
        }

        private async void OnFilesLastUpdatedChanged(object sender, PlaylistChangeArgs<DateTime?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.FilesLastUpdatedChanged), payload);
        }

        private async void OnSongsLastUpdatedChanged(object sender, PlaylistChangeArgs<DateTime?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.SongsLastUpdatedChanged), payload);
        }

        protected override async Task<byte[]> OnMessageReceived(string subTopic, byte[] payload)
        {
            Guid playlistId;
            ByteQueue queue = payload;
            switch (subTopic)
            {
                case nameof(playlistsRepo.GetPlaylist):
                    playlistId = queue.DequeueGuid();
                    Playlist playlist = await playlistsRepo.GetPlaylist(playlistId);
                    return new ByteQueue()
                        .Enqueue(playlist);

                case nameof(playlistsRepo.InsertPlaylist):
                    int? insertIndex = queue.DequeueIntNullable();
                    Playlist insertPlaylist = queue.DequeuePlaylist();
                    await playlistsRepo.InsertPlaylist(insertPlaylist, insertIndex);
                    return null;

                case nameof(playlistsRepo.RemovePlaylist):
                    playlistId = queue.DequeueGuid();
                    await playlistsRepo.RemovePlaylist(playlistId);
                    return null;

                case nameof(playlistsRepo.SetName):
                    playlistId = queue.DequeueGuid();
                    string name = queue.DequeueString();
                    await playlistsRepo.SetName(playlistId, name);
                    return null;

                case nameof(playlistsRepo.SetShuffle):
                    playlistId = queue.DequeueGuid();
                    OrderType shuffle = queue.DequeueOrderType();
                    await playlistsRepo.SetShuffle(playlistId, shuffle);
                    return null;

                case nameof(playlistsRepo.SetLoop):
                    playlistId = queue.DequeueGuid();
                    LoopType loop = queue.DequeueLoopType();
                    await playlistsRepo.SetLoop(playlistId, loop);
                    return null;

                case nameof(playlistsRepo.SetPlaybackRate):
                    playlistId = queue.DequeueGuid();
                    double playbackRate = queue.DequeueDouble();
                    await playlistsRepo.SetPlaybackRate(playlistId, playbackRate);
                    return null;

                case nameof(playlistsRepo.SetCurrentSongRequest):
                    playlistId = queue.DequeueGuid();
                    SongRequest? songRequest = queue.DequeueRequestSongNullable();
                    await playlistsRepo.SetCurrentSongRequest(playlistId, songRequest);
                    return null;

                case nameof(playlistsRepo.SetSongs):
                    playlistId = queue.DequeueGuid();
                    Song[] songs = queue.DequeueSongs();
                    await playlistsRepo.SetSongs(playlistId, songs);
                    return null;

                case nameof(playlistsRepo.GetFileMediaSourceRoots):
                    ICollection<FileMediaSourceRoot> roots = await playlistsRepo.GetFileMediaSourceRoots();
                    return new ByteQueue()
                        .Enqueue(roots);

                case nameof(playlistsRepo.GetFileMediaSourcesOfRoot):
                    Guid rootId = queue.DequeueGuid();
                    ICollection<FileMediaSource> sources = await playlistsRepo.GetFileMediaSourcesOfRoot(rootId);
                    return new ByteQueue()
                        .Enqueue(sources);

                case nameof(playlistsRepo.SetFileMedisSources):
                    playlistId = queue.DequeueGuid();
                    FileMediaSources fileMediaSources = queue.DequeueFileMediaSources();
                    await playlistsRepo.SetFileMedisSources(playlistId, fileMediaSources);
                    return null;

                case nameof(playlistsRepo.SetFilesLastUpdated):
                    playlistId = queue.DequeueGuid();
                    DateTime? filesLastUpdated = queue.DequeueDateTimeNullable();
                    await playlistsRepo.SetFilesLastUpdated(playlistId, filesLastUpdated);
                    return null;

                case nameof(playlistsRepo.SetSongsLastUpdated):
                    playlistId = queue.DequeueGuid();
                    DateTime? songsLastUpdated = queue.DequeueDateTimeNullable();
                    await playlistsRepo.SetSongsLastUpdated(playlistId, songsLastUpdated);
                    return null;

                default:
                    throw new NotSupportedException($"Received action is not supported: {subTopic}");
            }
        }
    }
}
