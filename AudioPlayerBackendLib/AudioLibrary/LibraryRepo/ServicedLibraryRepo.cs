using AudioPlayerBackend.Player;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public event EventHandler<AudioLibraryChangeArgs<DateTime?>> OnFoldersLastUpdatedChange;

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

        private void ForEachRepo(Action<ServicedLibraryRepo> action)
        {
            parent.GetRepos().OfType<ServicedLibraryRepo>().ForEach(action);
        }

        public async Task SendPlayStateChange(PlaybackState playState)
        {
            await baseRepo.SendPlayStateChange(playState);

            var args = new AudioLibraryChangeArgs<PlaybackState>(playState);
            ForEachRepo(repo => repo.OnPlayStateChange?.Invoke(this, args));
        }

        public async Task SendVolumeChange(double volume)
        {
            await baseRepo.SendVolumeChange(volume);

            var args = new AudioLibraryChangeArgs<double>(volume);
            ForEachRepo(repo => repo.OnVolumeChange?.Invoke(this, args));
        }

        public async Task SendCurrentPlaylistIdChange(Guid? currentPlaylistId)
        {
            await baseRepo.SendCurrentPlaylistIdChange(currentPlaylistId);

            var args = new AudioLibraryChangeArgs<Guid?>(currentPlaylistId);
            ForEachRepo(repo => repo.OnCurrentPlaylistIdChange?.Invoke(this, args));
        }

        public async Task SendFoldersLastUpdatedChange(DateTime? foldersLastUpdated)
        {
            await baseRepo.SendFoldersLastUpdatedChange(foldersLastUpdated);

            var args = new AudioLibraryChangeArgs<DateTime?>(foldersLastUpdated);
            ForEachRepo(repo => repo.OnFoldersLastUpdatedChange?.Invoke(this, args));
        }

        public void Dispose()
        {
            parent.AddRepo(this);
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
