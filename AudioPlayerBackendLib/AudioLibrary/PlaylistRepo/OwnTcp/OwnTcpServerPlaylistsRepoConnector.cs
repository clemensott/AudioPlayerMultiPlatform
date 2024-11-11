using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp
{
    internal class OwnTcpServerPlaylistsRepoConnector : IServerPlaylistsRepoConnector
    {
        private readonly IPlaylistsRepo playlistsRepo;
        private readonly IServerCommunicator serverCommunicator;

        public OwnTcpServerPlaylistsRepoConnector(IPlaylistsRepo playlistsRepo, IServerCommunicator serverCommunicator)
        {
            this.playlistsRepo = playlistsRepo;
            this.serverCommunicator = serverCommunicator;
        }

        public Task<byte[]> SendAsync(string funcName, byte[] payload = null)
        {
            return serverCommunicator.SendAsync($"{nameof(IPlaylistsRepo)}.{funcName}", payload);
        }

        public Task Start()
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

            serverCommunicator.Received += OnReceived;

            return Task.CompletedTask;
        }

        public Task Stop()
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

            serverCommunicator.Received -= OnReceived;

            return Task.CompletedTask;
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

        private async void OnReceived(object sender, ReceivedEventArgs e)
        {
            TaskCompletionSource<byte[]> anwser = null;
            try
            {
                string[] parts = e.Topic.Split('.');
                if (parts.Length != 2 || parts[0] != nameof(IPlaylistsRepo)) return;

                anwser = e.StartAnwser();

                Guid playlistId;
                ByteQueue payload = e.Payload;
                ByteQueue result;
                switch (parts[1])
                {
                    case nameof(playlistsRepo.GetPlaylist):
                        playlistId = payload.DequeueGuid();
                        Playlist playlist = await playlistsRepo.GetPlaylist(playlistId);
                        result = new ByteQueue()
                            .Enqueue(playlist);
                        anwser.SetResult(result);
                        break;

                    case nameof(playlistsRepo.InsertPlaylist):
                        int? insertIndex = payload.DequeueIntNullable();
                        Playlist insertPlaylist = payload.DequeuePlaylist();
                        await playlistsRepo.InsertPlaylist(insertPlaylist, insertIndex);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.RemovePlaylist):
                        playlistId = payload.DequeueGuid();
                        await playlistsRepo.RemovePlaylist(playlistId);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SetName):
                        playlistId = payload.DequeueGuid();
                        string name = payload.DequeueString();
                        await playlistsRepo.SetName(playlistId, name);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SetShuffle):
                        playlistId = payload.DequeueGuid();
                        OrderType shuffle = payload.DequeueOrderType();
                        await playlistsRepo.SetShuffle(playlistId, shuffle);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SetLoop):
                        playlistId = payload.DequeueGuid();
                        LoopType loop = payload.DequeueLoopType();
                        await playlistsRepo.SetLoop(playlistId, loop);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SetPlaybackRate):
                        playlistId = payload.DequeueGuid();
                        double playbackRate = payload.DequeueDouble();
                        await playlistsRepo.SetPlaybackRate(playlistId, playbackRate);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SetCurrentSongRequest):
                        playlistId = payload.DequeueGuid();
                        SongRequest? songRequest = payload.DequeueRequestSongNullable();
                        await playlistsRepo.SetCurrentSongRequest(playlistId, songRequest);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SetSongs):
                        playlistId = payload.DequeueGuid();
                        Song[] songs = payload.DequeueSongs();
                        await playlistsRepo.SetSongs(playlistId, songs);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.GetFileMediaSourceRoots):
                        ICollection<FileMediaSourceRoot> roots = await playlistsRepo.GetFileMediaSourceRoots();
                        result = new ByteQueue()
                            .Enqueue(roots);
                        anwser.SetResult(result);
                        break;
                        
                    case nameof(playlistsRepo.GetFileMediaSourcesOfRoot):
                        Guid rootId = payload.DequeueGuid();
                        ICollection<FileMediaSource> sources = await playlistsRepo.GetFileMediaSourcesOfRoot(rootId);
                        result = new ByteQueue()
                            .Enqueue(sources);
                        anwser.SetResult(result);
                        break;

                    case nameof(playlistsRepo.SetFileMedisSources):
                        playlistId = payload.DequeueGuid();
                        FileMediaSources fileMediaSources = payload.DequeueFileMediaSources();
                        await playlistsRepo.SetFileMedisSources(playlistId, fileMediaSources);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SetFilesLastUpdated):
                        playlistId = payload.DequeueGuid();
                        DateTime? filesLastUpdated = payload.DequeueDateTimeNullable();
                        await playlistsRepo.SetFilesLastUpdated(playlistId, filesLastUpdated);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SetSongsLastUpdated):
                        playlistId = payload.DequeueGuid();
                        DateTime? songsLastUpdated = payload.DequeueDateTimeNullable();
                        await playlistsRepo.SetSongsLastUpdated(playlistId, songsLastUpdated);
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
