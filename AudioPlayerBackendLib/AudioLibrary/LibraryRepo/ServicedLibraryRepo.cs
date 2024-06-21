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

        public event EventHandler<AudioLibraryChange<bool>> OnIsSearchChange;
        public event EventHandler<AudioLibraryChange<bool>> OnIsSearchShuffleChange;
        public event EventHandler<AudioLibraryChange<string>> OnSearchKeyChange;
        public event EventHandler<AudioLibraryChange<PlaybackState>> OnPlayStateChange;
        public event EventHandler<AudioLibraryChange<double>> OnVolumeChange;
        public event EventHandler<AudioLibraryChange<Guid?>> OnCurrentPlaylistIdChange;
        public event EventHandler<AudioLibraryChange<IList<PlaylistInfo>>> OnPlaylistsChange;
        public event EventHandler<AudioLibraryChange<IList<FileMediaSourceRoot>>> OnFileMediaSourceRootsChange;

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
            var args = new AudioLibraryChange<PlaybackState>(playState);
            ForEachRepoExcept(repo => repo.OnPlayStateChange?.Invoke(this, args));
            return baseRepo.SendPlayStateChange(playState);
        }

        public Task SendVolumeChange(double volume)
        {
            var args = new AudioLibraryChange<double>(volume);
            ForEachRepoExcept(repo => repo.OnVolumeChange?.Invoke(this, args));
            return baseRepo.SendVolumeChange(volume);
        }

        public Task SendCurrentPlaylistIdChange(Guid? currentPlaylistId)
        {
            var args = new AudioLibraryChange<Guid?>(currentPlaylistId);
            ForEachRepoExcept(repo => repo.OnCurrentPlaylistIdChange?.Invoke(this, args));
            return baseRepo.SendCurrentPlaylistIdChange(currentPlaylistId);
        }

        public Task SendPlaylistsChange(IList<PlaylistInfo> playlists)
        {
            var args = new AudioLibraryChange<IList<PlaylistInfo>>(playlists);
            ForEachRepoExcept(repo => repo.OnPlaylistsChange?.Invoke(this, args));
            return baseRepo.SendPlaylistsChange(playlists);
        }

        public Task SendFileMediaSourceRootsChange(IList<FileMediaSourceRoot> fileMediaSourceRoots)
        {
            var args = new AudioLibraryChange<IList<FileMediaSourceRoot>>(fileMediaSourceRoots);
            ForEachRepoExcept(repo => repo.OnFileMediaSourceRootsChange?.Invoke(this, args));
            return baseRepo.SendFileMediaSourceRootsChange(fileMediaSourceRoots);
        }

        public void Dispose()
        {
            parent.AddRepo(this);
        }
    }
}
