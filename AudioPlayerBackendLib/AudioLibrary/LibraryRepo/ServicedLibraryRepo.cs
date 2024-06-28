using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    class ServicedLibraryRepo : IServicedLibraryRepo
    {
        private readonly ILibraryRepo baseRepo;
        private readonly ILibraryRepoService parent;

        public event EventHandler<AudioLibraryChangeArgs<bool>> OnIsSearchChange;
        public event EventHandler<AudioLibraryChangeArgs<bool>> OnIsSearchShuffleChange;
        public event EventHandler<AudioLibraryChangeArgs<string>> OnSearchKeyChange;
        public event EventHandler<AudioLibraryChangeArgs<PlaybackState>> OnPlayStateChange;
        public event EventHandler<AudioLibraryChangeArgs<double>> OnVolumeChange;
        public event EventHandler<AudioLibraryChangeArgs<Guid?>> OnCurrentPlaylistIdChange;
        public event EventHandler<AudioLibraryChangeArgs<IList<PlaylistInfo>>> OnPlaylistsChange;
        public event EventHandler<AudioLibraryChangeArgs<IList<FileMediaSourceRoot>>> OnFileMediaSourceRootsChange;

        public ServicedLibraryRepo(ILibraryRepo baseRepo, ILibraryRepoService parent)
        {
            this.baseRepo = baseRepo;
            this.parent = parent;
            parent.AddRepo(this);
        }

        public Task<Library> GetLibrary()
        {
            return baseRepo.GetLibrary();
        }

        private void ForEachRepoExcept(Action<ServicedLibraryRepo> action)
        {
            parent.ForEachRepoExcept(repo => action(repo as ServicedLibraryRepo), this);
        }

        public Task SendPlayStateChange(PlaybackState playState)
        {
            var args = new AudioLibraryChangeArgs<PlaybackState>(playState);
            ForEachRepoExcept(repo => repo.OnPlayStateChange?.Invoke(this, args));
            return baseRepo.SendPlayStateChange(playState);
        }

        public Task SendVolumeChange(double volume)
        {
            var args = new AudioLibraryChangeArgs<double>(volume);
            ForEachRepoExcept(repo => repo.OnVolumeChange?.Invoke(this, args));
            return baseRepo.SendVolumeChange(volume);
        }

        public Task SendCurrentPlaylistIdChange(Guid? currentPlaylistId)
        {
            var args = new AudioLibraryChangeArgs<Guid?>(currentPlaylistId);
            ForEachRepoExcept(repo => repo.OnCurrentPlaylistIdChange?.Invoke(this, args));
            return baseRepo.SendCurrentPlaylistIdChange(currentPlaylistId);
        }

        public Task SendPlaylistsChange(IList<PlaylistInfo> playlists)
        {
            var args = new AudioLibraryChangeArgs<IList<PlaylistInfo>>(playlists);
            ForEachRepoExcept(repo => repo.OnPlaylistsChange?.Invoke(this, args));
            return baseRepo.SendPlaylistsChange(playlists);
        }

        public Task SendFileMediaSourceRootsChange(IList<FileMediaSourceRoot> fileMediaSourceRoots)
        {
            var args = new AudioLibraryChangeArgs<IList<FileMediaSourceRoot>>(fileMediaSourceRoots);
            ForEachRepoExcept(repo => repo.OnFileMediaSourceRootsChange?.Invoke(this, args));
            return baseRepo.SendFileMediaSourceRootsChange(fileMediaSourceRoots);
        }

        public void Dispose()
        {
            parent.AddRepo(this);
        }
    }
}
