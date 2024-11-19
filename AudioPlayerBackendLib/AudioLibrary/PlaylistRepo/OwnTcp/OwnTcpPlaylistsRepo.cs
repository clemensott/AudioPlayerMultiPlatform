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

        public event EventHandler<PlaylistChangeArgs<string>> NameChanged;
        public event EventHandler<PlaylistChangeArgs<OrderType>> ShuffleChanged;
        public event EventHandler<PlaylistChangeArgs<LoopType>> LoopChanged;
        public event EventHandler<PlaylistChangeArgs<double>> PlaybackRateChanged;
        public event EventHandler<PlaylistChangeArgs<SongRequest?>> CurrentSongRequestChanged;
        public event EventHandler<PlaylistChangeArgs<ICollection<Song>>> SongsChanged;
        public event EventHandler<InsertPlaylistArgs> InsertedPlaylist;
        public event EventHandler<RemovePlaylistArgs> RemovedPlaylist;
        public event EventHandler<PlaylistChangeArgs<FileMediaSources>> FileMedisSourcesChanged;
        public event EventHandler<PlaylistChangeArgs<DateTime?>> FilesLastUpdatedChanged;
        public event EventHandler<PlaylistChangeArgs<DateTime?>> SongsLastUpdatedChanged;

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
                    case nameof(NameChanged):
                        playlistId = payload.DequeueGuid();
                        string name = payload.DequeueString();
                        NameChanged?.Invoke(this, new PlaylistChangeArgs<string>(playlistId, name));
                        anwser.SetResult(null);
                        break;

                    case nameof(ShuffleChanged):
                        playlistId = payload.DequeueGuid();
                        OrderType shuffle = payload.DequeueOrderType();
                        ShuffleChanged?.Invoke(this, new PlaylistChangeArgs<OrderType>(playlistId, shuffle));
                        anwser.SetResult(null);
                        break;

                    case nameof(LoopChanged):
                        playlistId = payload.DequeueGuid();
                        LoopType loop = payload.DequeueLoopType();
                        LoopChanged?.Invoke(this, new PlaylistChangeArgs<LoopType>(playlistId, loop));
                        anwser.SetResult(null);
                        break;

                    case nameof(PlaybackRateChanged):
                        playlistId = payload.DequeueGuid();
                        double playbackRate = payload.DequeueDouble();
                        PlaybackRateChanged?.Invoke(this, new PlaylistChangeArgs<double>(playlistId, playbackRate));
                        anwser.SetResult(null);
                        break;

                    case nameof(CurrentSongRequestChanged):
                        playlistId = payload.DequeueGuid();
                        SongRequest? songRequest = payload.DequeueRequestSongNullable();
                        CurrentSongRequestChanged?.Invoke(this, new PlaylistChangeArgs<SongRequest?>(playlistId, songRequest));
                        anwser.SetResult(null);
                        break;

                    case nameof(SongsChanged):
                        playlistId = payload.DequeueGuid();
                        Song[] songs = payload.DequeueSongs();
                        SongsChanged?.Invoke(this, new PlaylistChangeArgs<ICollection<Song>>(playlistId, songs));
                        anwser.SetResult(null);
                        break;

                    case nameof(InsertedPlaylist):
                        int? insertIndex = payload.DequeueIntNullable();
                        Playlist insertPlaylist = payload.DequeuePlaylist();
                        InsertedPlaylist?.Invoke(this, new InsertPlaylistArgs(insertIndex, insertPlaylist));
                        anwser.SetResult(null);
                        break;

                    case nameof(RemovedPlaylist):
                        playlistId = payload.DequeueGuid();
                        RemovedPlaylist?.Invoke(this, new RemovePlaylistArgs(playlistId));
                        anwser.SetResult(null);
                        break;

                    case nameof(FileMedisSourcesChanged):
                        playlistId = payload.DequeueGuid();
                        FileMediaSources fileMediaSources = payload.DequeueFileMediaSources();
                        FileMedisSourcesChanged?.Invoke(this, new PlaylistChangeArgs<FileMediaSources>(playlistId, fileMediaSources));
                        anwser.SetResult(null);
                        break;

                    case nameof(FilesLastUpdatedChanged):
                        playlistId = payload.DequeueGuid();
                        DateTime? filesLastUpdated = payload.DequeueDateTimeNullable();
                        FilesLastUpdatedChanged?.Invoke(this, new PlaylistChangeArgs<DateTime?>(playlistId, filesLastUpdated));
                        anwser.SetResult(null);
                        break;

                    case nameof(SongsLastUpdatedChanged):
                        playlistId = payload.DequeueGuid();
                        DateTime? songsLastUpdated = payload.DequeueDateTimeNullable();
                        SongsLastUpdatedChanged?.Invoke(this, new PlaylistChangeArgs<DateTime?>(playlistId, songsLastUpdated));
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

        public async Task InsertPlaylist(Playlist playlist, int? index)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(index)
                .Enqueue(playlist);

            await SendAsync(nameof(InsertPlaylist), payload);

            InsertedPlaylist?.Invoke(this, new InsertPlaylistArgs(index, playlist));
        }

        public async Task RemovePlaylist(Guid id)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id);

            await SendAsync(nameof(RemovePlaylist), payload);

            RemovedPlaylist?.Invoke(id, new RemovePlaylistArgs(id));
        }

        public async Task SetName(Guid id, string name)
        {
            ByteQueue payload = new ByteQueue()
              .Enqueue(id)
              .Enqueue(name);

            await SendAsync(nameof(SetName), payload);

            NameChanged?.Invoke(this, new PlaylistChangeArgs<string>(id, name));
        }

        public async Task SetShuffle(Guid id, OrderType shuffle)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(shuffle);

            await SendAsync(nameof(SetShuffle), payload);

            ShuffleChanged?.Invoke(this, new PlaylistChangeArgs<OrderType>(id, shuffle));
        }

        public async Task SetLoop(Guid id, LoopType loop)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(loop);

            await SendAsync(nameof(SetLoop), payload);

            LoopChanged?.Invoke(this, new PlaylistChangeArgs<LoopType>(id, loop));
        }

        public async Task SetPlaybackRate(Guid id, double playbackRate)
        {
            ByteQueue payload = new ByteQueue()
               .Enqueue(id)
               .Enqueue(playbackRate);

            await SendAsync(nameof(SetPlaybackRate), payload);

            PlaybackRateChanged?.Invoke(this, new PlaylistChangeArgs<double>(id, playbackRate));
        }

        public async Task SetCurrentSongRequest(Guid id, SongRequest? songRequest)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(songRequest);

            await SendAsync(nameof(SetCurrentSongRequest), payload);

            CurrentSongRequestChanged?.Invoke(this, new PlaylistChangeArgs<SongRequest?>(id, songRequest));
        }

        public async Task SetSongs(Guid id, ICollection<Song> songs)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(songs);

            await SendAsync(nameof(SetSongs), payload);

            SongsChanged?.Invoke(this, new PlaylistChangeArgs<ICollection<Song>>(id, songs));
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

        public async Task SetFileMedisSources(Guid id, FileMediaSources fileMediaSources)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(fileMediaSources);

            await SendAsync(nameof(SetFileMedisSources), payload);

            FileMedisSourcesChanged?.Invoke(this, new PlaylistChangeArgs<FileMediaSources>(id, fileMediaSources));
        }

        public async Task SetFilesLastUpdated(Guid id, DateTime? filesLastUpdated)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(filesLastUpdated);

            await SendAsync(nameof(SetFilesLastUpdated), payload);

            FilesLastUpdatedChanged?.Invoke(this, new PlaylistChangeArgs<DateTime?>(id, filesLastUpdated));
        }

        public async Task SetSongsLastUpdated(Guid id, DateTime? songsLastUpdated)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(id)
                .Enqueue(songsLastUpdated);

            await SendAsync(nameof(SetSongsLastUpdated), payload);

            SongsLastUpdatedChanged?.Invoke(this, new PlaylistChangeArgs<DateTime?>(id, songsLastUpdated));
        }
    }
}
