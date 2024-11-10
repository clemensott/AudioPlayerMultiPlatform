using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp
{
    internal class OwnTcpPlaylistsRepo : IPlaylistsRepo
    {
        private readonly IClientCommunicator clientCommunicator;

        public event EventHandler<PlaylistChangeArgs<string>> OnNameChange;
        public event EventHandler<PlaylistChangeArgs<OrderType>> OnShuffleChange;
        public event EventHandler<PlaylistChangeArgs<LoopType>> OnLoopChange;
        public event EventHandler<PlaylistChangeArgs<double>> OnPlaybackRateChange;
        public event EventHandler<PlaylistChangeArgs<TimeSpan>> OnPositionChange;
        public event EventHandler<PlaylistChangeArgs<TimeSpan>> OnDurationChange;
        public event EventHandler<PlaylistChangeArgs<RequestSong?>> OnRequestSongChange;
        public event EventHandler<PlaylistChangeArgs<Guid?>> OnCurrentSongIdChange;
        public event EventHandler<PlaylistChangeArgs<ICollection<Song>>> OnSongsChange;
        public event EventHandler<InsertPlaylistArgs> OnInsertPlaylist;
        public event EventHandler<RemovePlaylistArgs> OnRemovePlaylist;
        public event EventHandler<PlaylistChangeArgs<FileMediaSources>> OnFileMedisSourcesChange;
        public event EventHandler<PlaylistChangeArgs<DateTime?>> OnFilesLastUpdatedChange;
        public event EventHandler<PlaylistChangeArgs<DateTime?>> OnSongsLastUpdatedChange;

        public OwnTcpPlaylistsRepo(IClientCommunicator clientCommunicator)
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
                if (parts.Length != 2 || parts[0] != nameof(IPlaylistsRepo)) return;

                anwser = e.StartAnwser();

                Guid playlistId;
                ByteQueue payload = e.Payload;
                switch (parts[1])
                {
                    case nameof(OnNameChange):
                        playlistId = payload.DequeueGuid();
                        string name = payload.DequeueString();
                        OnNameChange?.Invoke(this, new PlaylistChangeArgs<string>(playlistId, name));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnShuffleChange):
                        playlistId = payload.DequeueGuid();
                        OrderType shuffle = payload.DequeueOrderType();
                        OnShuffleChange?.Invoke(this, new PlaylistChangeArgs<OrderType>(playlistId, shuffle));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnLoopChange):
                        playlistId = payload.DequeueGuid();
                        LoopType loop = payload.DequeueLoopType();
                        OnLoopChange?.Invoke(this, new PlaylistChangeArgs<LoopType>(playlistId, loop));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnPlaybackRateChange):
                        playlistId = payload.DequeueGuid();
                        double playbackRate = payload.DequeueDouble();
                        OnPlaybackRateChange?.Invoke(this, new PlaylistChangeArgs<double>(playlistId, playbackRate));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnPositionChange):
                        playlistId = payload.DequeueGuid();
                        TimeSpan position = payload.DequeueTimeSpan();
                        OnPositionChange?.Invoke(this, new PlaylistChangeArgs<TimeSpan>(playlistId, position));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnDurationChange):
                        playlistId = payload.DequeueGuid();
                        TimeSpan duration = payload.DequeueTimeSpan();
                        OnDurationChange?.Invoke(this, new PlaylistChangeArgs<TimeSpan>(playlistId, duration));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnRequestSongChange):
                        playlistId = payload.DequeueGuid();
                        RequestSong? requestSong = payload.DequeueRequestSongNullable();
                        OnRequestSongChange?.Invoke(this, new PlaylistChangeArgs<RequestSong?>(playlistId, requestSong));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnCurrentSongIdChange):
                        playlistId = payload.DequeueGuid();
                        Guid? currentSongId = payload.DequeueGuidNullable();
                        OnCurrentSongIdChange?.Invoke(this, new PlaylistChangeArgs<Guid?>(playlistId, currentSongId));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnSongsChange):
                        playlistId = payload.DequeueGuid();
                        Song[] songs = payload.DequeueSongs();
                        OnSongsChange?.Invoke(this, new PlaylistChangeArgs<ICollection<Song>>(playlistId, songs));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnInsertPlaylist):
                        int? insertIndex = payload.DequeueIntNullable();
                        Playlist insertPlaylist = payload.DequeuePlaylist();
                        OnInsertPlaylist?.Invoke(this, new InsertPlaylistArgs(insertIndex, insertPlaylist));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnRemovePlaylist):
                        playlistId = payload.DequeueGuid();
                        OnRemovePlaylist?.Invoke(this, new RemovePlaylistArgs(playlistId));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnFileMedisSourcesChange):
                        playlistId = payload.DequeueGuid();
                        FileMediaSources fileMediaSources = payload.DequeueFileMediaSources();
                        OnFileMedisSourcesChange?.Invoke(this, new PlaylistChangeArgs<FileMediaSources>(playlistId, fileMediaSources));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnFilesLastUpdatedChange):
                        playlistId = payload.DequeueGuid();
                        DateTime? filesLastUpdated = payload.DequeueDateTimeNullable();
                        OnFilesLastUpdatedChange?.Invoke(this, new PlaylistChangeArgs<DateTime?>(playlistId, filesLastUpdated));
                        anwser.SetResult(null);
                        break;

                    case nameof(OnSongsLastUpdatedChange):
                        playlistId = payload.DequeueGuid();
                        DateTime? songsLastUpdated = payload.DequeueDateTimeNullable();
                        OnSongsLastUpdatedChange?.Invoke(this, new PlaylistChangeArgs<DateTime?>(playlistId, songsLastUpdated));
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
            return clientCommunicator.SendAsync($"{nameof(IPlaylistsRepo)}.{funcName}", payload);
        }

        public async Task<Playlist> GetPlaylist(Guid id)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id);

            ByteQueue result = await SendAsync(nameof(GetPlaylist), payload);

            return result.DequeuePlaylist();
        }

        public async Task SendInsertPlaylist(Playlist playlist, int? index)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(index)
                .Enqueue(playlist);

            await SendAsync(nameof(SendInsertPlaylist), payload);

            OnInsertPlaylist?.Invoke(this, new InsertPlaylistArgs(index, playlist));
        }

        public async Task SendRemovePlaylist(Guid id)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id);

            await SendAsync(nameof(SendRemovePlaylist), payload);

            OnRemovePlaylist?.Invoke(id, new RemovePlaylistArgs(id));
        }

        public async Task SendNameChange(Guid id, string name)
        {
            ByteQueue payload = new ByteQueue()
              .Enqueue(id)
              .Enqueue(name);

            await SendAsync(nameof(SendNameChange), payload);

            OnNameChange?.Invoke(this, new PlaylistChangeArgs<string>(id, name));
        }

        public async Task SendShuffleChange(Guid id, OrderType shuffle)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(shuffle);

            await SendAsync(nameof(SendShuffleChange), payload);

            OnShuffleChange?.Invoke(this, new PlaylistChangeArgs<OrderType>(id, shuffle));
        }

        public async Task SendLoopChange(Guid id, LoopType loop)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(loop);

            await SendAsync(nameof(SendLoopChange), payload);

            OnLoopChange?.Invoke(this, new PlaylistChangeArgs<LoopType>(id, loop));
        }

        public async Task SendPlaybackRateChange(Guid id, double playbackRate)
        {
            ByteQueue payload = new ByteQueue()
               .Enqueue(id)
               .Enqueue(playbackRate);

            await SendAsync(nameof(SendPlaybackRateChange), payload);

            OnPlaybackRateChange?.Invoke(this, new PlaylistChangeArgs<double>(id, playbackRate));
        }

        public async Task SendPositionChange(Guid id, TimeSpan position)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(position);

            await SendAsync(nameof(SendPositionChange), payload);

            OnPositionChange?.Invoke(this, new PlaylistChangeArgs<TimeSpan>(id, position));
        }

        public async Task SendDurationChange(Guid id, TimeSpan duration)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(duration);

            await SendAsync(nameof(SendDurationChange), payload);

            OnDurationChange?.Invoke(this, new PlaylistChangeArgs<TimeSpan>(id, duration));
        }

        public async Task SendCurrentSongIdChange(Guid id, Guid? currentSongId)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(currentSongId);

            await SendAsync(nameof(SendCurrentSongIdChange), payload);

            OnCurrentSongIdChange?.Invoke(this, new PlaylistChangeArgs<Guid?>(id, currentSongId));
        }

        public async Task SendRequestSongChange(Guid id, RequestSong? requestSong)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(requestSong);

            await SendAsync(nameof(SendRequestSongChange), payload);

            OnRequestSongChange?.Invoke(this, new PlaylistChangeArgs<RequestSong?>(id, requestSong));
        }

        public async Task SendSongsChange(Guid id, ICollection<Song> songs)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(songs);

            await SendAsync(nameof(SendSongsChange), payload);

            OnSongsChange?.Invoke(this, new PlaylistChangeArgs<ICollection<Song>>(id, songs));
        }

        public async Task<ICollection<FileMediaSourceRoot>> GetFileMediaSourceRoots()
        {
            ByteQueue result = await SendAsync(nameof(GetFileMediaSourceRoots), null);

            return result.DequeueFileMediaSourceRoots();
        }

        public async Task<ICollection<FileMediaSource>> GetFileMediaSourcesOfRoot(Guid rootId)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(rootId);

            ByteQueue result = await SendAsync(nameof(GetFileMediaSourcesOfRoot), payload);

            return result.DequeueFileMediaSourceArray();
        }

        public async Task SendFileMedisSourcesChange(Guid id, FileMediaSources fileMediaSources)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(fileMediaSources);

            await SendAsync(nameof(SendFileMedisSourcesChange), payload);

            OnFileMedisSourcesChange?.Invoke(this, new PlaylistChangeArgs<FileMediaSources>(id, fileMediaSources));
        }

        public async Task SendFilesLastUpdatedChange(Guid id, DateTime? filesLastUpdated)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(filesLastUpdated);

            await SendAsync(nameof(SendFilesLastUpdatedChange), payload);

            OnFilesLastUpdatedChange?.Invoke(this, new PlaylistChangeArgs<DateTime?>(id, filesLastUpdated));
        }

        public async Task SendSongsLastUpdatedChange(Guid id, DateTime? songsLastUpdated)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(songsLastUpdated);

            await SendAsync(nameof(SendSongsLastUpdatedChange), payload);

            OnSongsLastUpdatedChange?.Invoke(this, new PlaylistChangeArgs<DateTime?>(id, songsLastUpdated));
        }
    }
}
