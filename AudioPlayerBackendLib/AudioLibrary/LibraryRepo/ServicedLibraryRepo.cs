using AudioPlayerBackend.Player;
using StdOttStandard.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    class ServicedLibraryRepo : IServicedLibraryRepo
    {
        private readonly ILibraryRepo baseRepo;
        private readonly ILibraryRepoService parent;

        public event EventHandler<AudioLibraryChangeArgs<PlaybackState>> OnPlayStateChange;
        public event EventHandler<AudioLibraryChangeArgs<double>> OnVolumeChange;
        public event EventHandler<AudioLibraryChangeArgs<Guid?>> OnCurrentPlaylistIdChange;
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
            //parent.GetRepos().OfType<ServicedLibraryRepo>().ForEach(action);
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

        public Task Start()
        {
            baseRepo.OnPlayStateChange += BaseRepo_OnPlayStateChange;
            baseRepo.OnVolumeChange += BaseRepo_OnVolumeChange;
            baseRepo.OnCurrentPlaylistIdChange += BaseRepo_OnCurrentPlaylistIdChange;
            baseRepo.OnFoldersLastUpdatedChange += BaseRepo_OnFoldersLastUpdatedChange;

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            baseRepo.OnPlayStateChange -= BaseRepo_OnPlayStateChange;
            baseRepo.OnVolumeChange -= BaseRepo_OnVolumeChange;
            baseRepo.OnCurrentPlaylistIdChange -= BaseRepo_OnCurrentPlaylistIdChange;
            baseRepo.OnFoldersLastUpdatedChange -= BaseRepo_OnFoldersLastUpdatedChange;

            return Task.CompletedTask;
        }

        private void BaseRepo_OnPlayStateChange(object sender, AudioLibraryChangeArgs<PlaybackState> e)
        {
            OnPlayStateChange?.Invoke(this, e);
        }

        private void BaseRepo_OnVolumeChange(object sender, AudioLibraryChangeArgs<double> e)
        {
            OnVolumeChange?.Invoke(this, e);
        }

        private void BaseRepo_OnCurrentPlaylistIdChange(object sender, AudioLibraryChangeArgs<Guid?> e)
        {
            OnCurrentPlaylistIdChange?.Invoke(this, e);
        }

        private void BaseRepo_OnFoldersLastUpdatedChange(object sender, AudioLibraryChangeArgs<DateTime?> e)
        {
            OnFoldersLastUpdatedChange?.Invoke(this, e);
        }

        public async Task Dispose()
        {
            await Stop();
            parent.RemoveRepo(this);
        }
    }
}
