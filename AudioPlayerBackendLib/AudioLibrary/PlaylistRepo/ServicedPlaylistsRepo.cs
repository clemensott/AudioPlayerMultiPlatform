using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    class ServicedPlaylistsRepo : IServicedPlaylistsRepo
    {
        private readonly IPlaylistsRepo baseRepo;
        private readonly IPlaylistsRepoService parent;

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

        public ServicedPlaylistsRepo(IPlaylistsRepo baseRepo, IPlaylistsRepoService parent)
        {
            this.baseRepo = baseRepo;
            this.parent = parent;
            parent.AddRepo(this);
        }

        public Task<Playlist> GetPlaylist(Guid id)
        {
            return baseRepo.GetPlaylist(id);
        }

        private void ForEachRepo(Action<ServicedPlaylistsRepo> action)
        {
            parent.GetRepos().OfType<ServicedPlaylistsRepo>().ForEach(action);
        }

        public async Task SendInsertPlaylist(Playlist playlist, int index)
        {
            await baseRepo.SendInsertPlaylist(playlist, index);
            var args = new InsertPlaylistArgs(index, playlist);
            ForEachRepo(repo => repo.OnInsertPlaylist?.Invoke(this, args));
        }

        public async Task SendRemovePlaylist(Guid id)
        {
            await baseRepo.SendRemovePlaylist(id);
            var args = new RemovePlaylistArgs(id);
            ForEachRepo(repo => repo.OnRemovePlaylist?.Invoke(this, args));
        }

        public async Task SendNameChange(Guid id, string name)
        {
            await baseRepo.SendNameChange(id, name);
            var args = new PlaylistChangeArgs<string>(id, name);
            ForEachRepo(repo => repo.OnNameChange?.Invoke(this, args));
        }

        public async Task SendShuffleChange(Guid id, OrderType shuffle)
        {
            await baseRepo.SendShuffleChange(id, shuffle);
            var args = new PlaylistChangeArgs<OrderType>(id, shuffle);
            ForEachRepo(repo => repo.OnShuffleChange?.Invoke(this, args));
        }

        public async Task SendLoopChange(Guid id, LoopType loop)
        {
            await baseRepo.SendLoopChange(id, loop);
            var args = new PlaylistChangeArgs<LoopType>(id, loop);
            ForEachRepo(repo => repo.OnLoopChange?.Invoke(this, args));
        }

        public async Task SendPlaybackRateChange(Guid id, double playbackRate)
        {
            await baseRepo.SendPlaybackRateChange(id, playbackRate);
            var args = new PlaylistChangeArgs<double>(id, playbackRate);
            ForEachRepo(repo => repo.OnPlaybackRateChange?.Invoke(this, args));
        }

        public async Task SendPositionChange(Guid id, TimeSpan position)
        {
            await baseRepo.SendPositionChange(id, position);
            var args = new PlaylistChangeArgs<TimeSpan>(id, position);
            ForEachRepo(repo => repo.OnPositionChange?.Invoke(this, args));
        }

        public async Task SendDurationChange(Guid id, TimeSpan duration)
        {
            await baseRepo.SendDurationChange(id, duration);
            var args = new PlaylistChangeArgs<TimeSpan>(id, duration);
            ForEachRepo(repo => repo.OnDurationChange?.Invoke(this, args));
        }

        public async Task SendRequestSongChange(Guid id, RequestSong? requestSong)
        {
            await baseRepo.SendRequestSongChange(id, requestSong);
            var args = new PlaylistChangeArgs<RequestSong?>(id, requestSong);
            ForEachRepo(repo => repo.OnRequestSongChange?.Invoke(this, args));
        }

        public async Task SendCurrentSongIdChange(Guid id, Guid? currentSongId)
        {
            await baseRepo.SendCurrentSongIdChange(id, currentSongId);
            var args = new PlaylistChangeArgs<Guid?>(id, currentSongId);
            ForEachRepo(repo => repo.OnCurrentSongIdChange?.Invoke(this, args));
        }

        public async Task SendSongsChange(Guid id, ICollection<Song> songs)
        {
            await baseRepo.SendSongsChange(id, songs);
            var args = new PlaylistChangeArgs<ICollection<Song>>(id, songs);
            ForEachRepo(repo => repo.OnSongsChange?.Invoke(this, args));
        }

        public async Task SendFileMedisSourcesChange(Guid id, FileMediaSources fileMediaSources)
        {
            await baseRepo.SendFileMedisSourcesChange(id, fileMediaSources);
            var args = new PlaylistChangeArgs<FileMediaSources>(id, fileMediaSources);
            ForEachRepo(repo => repo.OnFileMedisSourcesChange?.Invoke(this, args));
        }

        public void Dispose()
        {
            parent.RemoveRepo(this);
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        Task IAudioService.Dispose()
        {
            Dispose();
            return Task.CompletedTask;
        }
    }
}
