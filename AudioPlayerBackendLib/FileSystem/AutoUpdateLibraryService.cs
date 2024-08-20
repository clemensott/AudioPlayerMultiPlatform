using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using StdOttStandard;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.FileSystem
{
    internal class AutoUpdateLibraryService : IAudioService
    {
        private const int timerInterval = 1000 * 60; // one minute
        private static readonly TimeSpan foldersUpdateInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan filesUpdateInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan songsUpdateInterval = TimeSpan.FromDays(1);

        private readonly IServicedLibraryRepo libraryRepo;
        private readonly IServicedPlaylistsRepo playlistRepo;
        private readonly IUpdateLibraryService updateLibraryService;
        private readonly Timer timer;

        private bool isCheckingForUpdates = false;

        public AutoUpdateLibraryService(IServicedLibraryRepo libraryRepo, IServicedPlaylistsRepo playlistRepo,
            IUpdateLibraryService updateLibraryService)
        {
            this.libraryRepo = libraryRepo;
            this.playlistRepo = playlistRepo;
            this.updateLibraryService = updateLibraryService;

            timer = new Timer(OnTick, null, Timeout.Infinite, timerInterval);
        }

        private async void OnTick(object sender)
        {
            await CheckNeededUpdates();
        }

        private static bool NeedsFoldersUpdate(DateTime? foldersLastUpdated)
        {
            return !foldersLastUpdated.TryHasValue(out DateTime lastUpdated)
                || DateTime.Now - lastUpdated > foldersUpdateInterval;
        }

        private static bool NeedsFilesUpdate(DateTime? filesLastUpdated)
        {
            return !filesLastUpdated.TryHasValue(out DateTime lastUpdated)
                || DateTime.Now - lastUpdated > filesUpdateInterval;
        }

        private static bool NeedsSongsUpdate(DateTime? songsLastUpdated)
        {
            return !songsLastUpdated.TryHasValue(out DateTime lastUpdated)
                || DateTime.Now - lastUpdated > songsUpdateInterval;
        }

        private async Task CheckNeededUpdates()
        {
            if (!isCheckingForUpdates) return;

            try
            {
                Library library = await libraryRepo.GetLibrary();
                if (NeedsFoldersUpdate(library.FoldersLastUpdated)) await updateLibraryService.UpdateLibrary();

                library = await libraryRepo.GetLibrary();
                foreach (PlaylistInfo playlist in library.Playlists)
                {
                    if (NeedsFilesUpdate(playlist.FilesLastUpdated)) await updateLibraryService.UpdateSourcePlaylist(playlist.Id);
                    if (NeedsSongsUpdate(playlist.SongsLastUpdated)) await updateLibraryService.ReloadSourcePlaylist(playlist.Id);
                }
            }
            finally
            {
                isCheckingForUpdates = false;
            }
        }

        public Task Start()
        {
            timer.Change(0, timerInterval);

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            timer.Change(Timeout.Infinite, timerInterval);

            return Task.CompletedTask;
        }

        public Task Dispose()
        {
            timer.Change(Timeout.Infinite, timerInterval);
            timer.Dispose();

            libraryRepo.Dispose();
            playlistRepo.Dispose();

            return Task.CompletedTask;
        }
    }
}
