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
            playlistsRepo.OnInsertPlaylist += OnInsertPlaylist;
            playlistsRepo.OnRemovePlaylist += OnRemovePlaylist;
            playlistsRepo.OnNameChange += OnNameChange;
            playlistsRepo.OnShuffleChange += OnShuffleChange;
            playlistsRepo.OnLoopChange += OnLoopChange;
            playlistsRepo.OnPlaybackRateChange += OnPlaybackRateChange;
            playlistsRepo.OnPositionChange += OnPositionChange;
            playlistsRepo.OnDurationChange += OnDurationChange;
            playlistsRepo.OnRequestSongChange += OnRequestSongChange;
            playlistsRepo.OnCurrentSongIdChange += OnCurrentSongIdChange;
            playlistsRepo.OnSongsChange += OnSongsChange;
            playlistsRepo.OnFileMedisSourcesChange += OnFileMedisSourcesChange;
            playlistsRepo.OnFilesLastUpdatedChange += OnFilesLastUpdatedChange;
            playlistsRepo.OnSongsLastUpdatedChange += OnSongsLastUpdatedChange;

            serverCommunicator.Received += OnReceived;

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            playlistsRepo.OnInsertPlaylist -= OnInsertPlaylist;
            playlistsRepo.OnRemovePlaylist -= OnRemovePlaylist;
            playlistsRepo.OnNameChange -= OnNameChange;
            playlistsRepo.OnShuffleChange -= OnShuffleChange;
            playlistsRepo.OnLoopChange -= OnLoopChange;
            playlistsRepo.OnPlaybackRateChange -= OnPlaybackRateChange;
            playlistsRepo.OnPositionChange -= OnPositionChange;
            playlistsRepo.OnDurationChange -= OnDurationChange;
            playlistsRepo.OnRequestSongChange -= OnRequestSongChange;
            playlistsRepo.OnCurrentSongIdChange -= OnCurrentSongIdChange;
            playlistsRepo.OnSongsChange -= OnSongsChange;
            playlistsRepo.OnFileMedisSourcesChange -= OnFileMedisSourcesChange;
            playlistsRepo.OnFilesLastUpdatedChange -= OnFilesLastUpdatedChange;
            playlistsRepo.OnSongsLastUpdatedChange -= OnSongsLastUpdatedChange;

            serverCommunicator.Received -= OnReceived;

            return Task.CompletedTask;
        }

        private async void OnInsertPlaylist(object sender, InsertPlaylistArgs e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Index)
                .Enqueue(e.Playlist);
            await SendAsync(nameof(playlistsRepo.OnInsertPlaylist), payload);
        }

        private async void OnRemovePlaylist(object sender, RemovePlaylistArgs e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id);
            await SendAsync(nameof(playlistsRepo.OnRemovePlaylist), payload);
        }

        private async void OnNameChange(object sender, PlaylistChangeArgs<string> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnNameChange), payload);
        }

        private async void OnShuffleChange(object sender, PlaylistChangeArgs<OrderType> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnShuffleChange), payload);
        }

        private async void OnLoopChange(object sender, PlaylistChangeArgs<LoopType> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnLoopChange), payload);
        }

        private async void OnPlaybackRateChange(object sender, PlaylistChangeArgs<double> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnPlaybackRateChange), payload);
        }

        private async void OnPositionChange(object sender, PlaylistChangeArgs<TimeSpan> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnPositionChange), payload);
        }

        private async void OnDurationChange(object sender, PlaylistChangeArgs<TimeSpan> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnDurationChange), payload);
        }

        private async void OnRequestSongChange(object sender, PlaylistChangeArgs<RequestSong?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnRequestSongChange), payload);
        }

        private async void OnCurrentSongIdChange(object sender, PlaylistChangeArgs<Guid?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnCurrentSongIdChange), payload);
        }

        private async void OnSongsChange(object sender, PlaylistChangeArgs<ICollection<Song>> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnSongsChange), payload);
        }

        private async void OnFileMedisSourcesChange(object sender, PlaylistChangeArgs<MediaSource.FileMediaSources> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnFileMedisSourcesChange), payload);
        }

        private async void OnFilesLastUpdatedChange(object sender, PlaylistChangeArgs<DateTime?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnFilesLastUpdatedChange), payload);
        }

        private async void OnSongsLastUpdatedChange(object sender, PlaylistChangeArgs<DateTime?> e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Id)
                .Enqueue(e.NewValue);
            await SendAsync(nameof(playlistsRepo.OnSongsLastUpdatedChange), payload);
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

                    case nameof(playlistsRepo.SendInsertPlaylist):
                        int insertIndex = payload.DequeueInt();
                        Playlist insertPlaylist = payload.DequeuePlaylist();
                        await playlistsRepo.SendInsertPlaylist(insertPlaylist, insertIndex);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendRemovePlaylist):
                        playlistId = payload.DequeueGuid();
                        await playlistsRepo.SendRemovePlaylist(playlistId);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendNameChange):
                        playlistId = payload.DequeueGuid();
                        string name = payload.DequeueString();
                        await playlistsRepo.SendNameChange(playlistId, name);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendShuffleChange):
                        playlistId = payload.DequeueGuid();
                        OrderType shuffle = payload.DequeueOrderType();
                        await playlistsRepo.SendShuffleChange(playlistId, shuffle);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendLoopChange):
                        playlistId = payload.DequeueGuid();
                        LoopType loop = payload.DequeueLoopType();
                        await playlistsRepo.SendLoopChange(playlistId, loop);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendPlaybackRateChange):
                        playlistId = payload.DequeueGuid();
                        double playbackRate = payload.DequeueDouble();
                        await playlistsRepo.SendPlaybackRateChange(playlistId, playbackRate);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendPositionChange):
                        playlistId = payload.DequeueGuid();
                        TimeSpan position = payload.DequeueTimeSpan();
                        await playlistsRepo.SendPositionChange(playlistId, position);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendDurationChange):
                        playlistId = payload.DequeueGuid();
                        TimeSpan duration = payload.DequeueTimeSpan();
                        await playlistsRepo.SendDurationChange(playlistId, duration);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendRequestSongChange):
                        playlistId = payload.DequeueGuid();
                        RequestSong? requestSong = payload.DequeueRequestSongNullable();
                        await playlistsRepo.SendRequestSongChange(playlistId, requestSong);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendCurrentSongIdChange):
                        playlistId = payload.DequeueGuid();
                        Guid? currentSongId = payload.DequeueGuidNullable();
                        await playlistsRepo.SendCurrentSongIdChange(playlistId, currentSongId);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendSongsChange):
                        playlistId = payload.DequeueGuid();
                        Song[] songs = payload.DequeueSongs();
                        await playlistsRepo.SendSongsChange(playlistId, songs);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.GetFileMediaSourcesOfRoot):
                        Guid rootId = payload.DequeueGuid();
                        ICollection<FileMediaSource> sources = await playlistsRepo.GetFileMediaSourcesOfRoot(rootId);
                        result = new ByteQueue()
                            .Enqueue(sources);
                        anwser.SetResult(result);
                        break;

                    case nameof(playlistsRepo.SendFileMedisSourcesChange):
                        playlistId = payload.DequeueGuid();
                        FileMediaSources fileMediaSources = payload.DequeueFileMediaSources();
                        await playlistsRepo.SendFileMedisSourcesChange(playlistId, fileMediaSources);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendFilesLastUpdatedChange):
                        playlistId = payload.DequeueGuid();
                        DateTime? filesLastUpdated = payload.DequeueDateTimeNullable();
                        await playlistsRepo.SendFilesLastUpdatedChange(playlistId, filesLastUpdated);
                        anwser.SetResult(null);
                        break;

                    case nameof(playlistsRepo.SendSongsLastUpdatedChange):
                        playlistId = payload.DequeueGuid();
                        DateTime? songsLastUpdated = payload.DequeueDateTimeNullable();
                        await playlistsRepo.SendSongsLastUpdatedChange(playlistId, songsLastUpdated);
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
