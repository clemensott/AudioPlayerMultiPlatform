using AudioPlayerBackend.Audio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    class ServicedPlaylistRepo : IServicedPlaylistRepo
    {
        private readonly IPlaylistsRepo baseRepo;
        private readonly IPlaylistsRepoService parent;

        public event EventHandler<PlaylistChange<string>> OnNameChange;
        public event EventHandler<PlaylistChange<OrderType>> OnShuffleChange;
        public event EventHandler<PlaylistChange<LoopType>> OnLoopChange;
        public event EventHandler<PlaylistChange<double>> OnPlaybackRateChange;
        public event EventHandler<PlaylistChange<TimeSpan>> OnPositionChange;
        public event EventHandler<PlaylistChange<TimeSpan>> OnDurationChange;
        public event EventHandler<PlaylistChange<RequestSong?>> OnRequestSongChange;
        public event EventHandler<PlaylistChange<Guid?>> OnCurrentSongIdChange;
        public event EventHandler<PlaylistChange<IList<Song>>> OnSongsChange;

        public ServicedPlaylistRepo(IPlaylistsRepo baseRepo, IPlaylistsRepoService parent)
        {
            this.baseRepo = baseRepo;
            this.parent = parent;
            parent.AddRepo(this);
        }

        public Task<Playlist> GetPlaylist(Guid id)
        {
            return baseRepo.GetPlaylist(id);
        }

        private void ForEachRepoExcept(Action<ServicedPlaylistRepo> action)
        {
            parent.ForEachRepoExcept(repo => action(repo as ServicedPlaylistRepo), this);
        }

        public Task SendNameChange(Guid id, string name)
        {
            var args = new PlaylistChange<string>(id, name);
            ForEachRepoExcept(repo => repo.OnNameChange?.Invoke(this, args));
            return baseRepo.SendNameChange(id, name);
        }

        public Task SendShuffleChange(Guid id, OrderType shuffle)
        {
            var args = new PlaylistChange<OrderType>(id, shuffle);
            ForEachRepoExcept(repo => repo.OnShuffleChange?.Invoke(this, args));
            return baseRepo.SendShuffleChange(id, shuffle);
        }

        public Task SendLoopChange(Guid id, LoopType loop)
        {
            var args = new PlaylistChange<LoopType>(id, loop);
            ForEachRepoExcept(repo => repo.OnLoopChange?.Invoke(this, args));
            return baseRepo.SendLoopChange(id, loop);
        }

        public Task SendPlaybackRateChange(Guid id, double playbackRate)
        {
            var args = new PlaylistChange<double>(id, playbackRate);
            ForEachRepoExcept(repo => repo.OnPlaybackRateChange?.Invoke(this, args));
            return baseRepo.SendPlaybackRateChange(id, playbackRate);
        }

        public Task SendPositionChange(Guid id, TimeSpan position)
        {
            var args = new PlaylistChange<TimeSpan>(id, position);
            ForEachRepoExcept(repo => repo.OnPositionChange?.Invoke(this, args));
            return baseRepo.SendPositionChange(id, position);
        }

        public Task SendDurationChange(Guid id, TimeSpan duration)
        {
            var args = new PlaylistChange<TimeSpan>(id, duration);
            ForEachRepoExcept(repo => repo.OnDurationChange?.Invoke(this, args));
            return baseRepo.SendDurationChange(id, duration);
        }

        public Task SendRequestSongChange(Guid id, RequestSong? requestSong)
        {
            var args = new PlaylistChange<RequestSong?>(id, requestSong);
            ForEachRepoExcept(repo => repo.OnRequestSongChange?.Invoke(this, args));
            return baseRepo.SendRequestSongChange(id, requestSong);
        }

        public Task SendCurrentSongIdChange(Guid id, Guid? currentSongId)
        {
            var args = new PlaylistChange<Guid?>(id, currentSongId);
            ForEachRepoExcept(repo => repo.OnCurrentSongIdChange?.Invoke(this, args));
            return baseRepo.SendCurrentSongIdChange(id, currentSongId);
        }

        public Task SendSongsChange(Guid id, IList<Song> songs)
        {
            var args = new PlaylistChange<IList<Song>>(id, songs);
            ForEachRepoExcept(repo => repo.OnSongsChange?.Invoke(this, args));
            return baseRepo.SendSongsChange(id, songs);
        }

        public void Dispose()
        {
            parent.RemoveRepo(this);
        }
    }
}
