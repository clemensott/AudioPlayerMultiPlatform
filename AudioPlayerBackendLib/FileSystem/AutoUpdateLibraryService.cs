using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.Extensions;
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

        private readonly ILibraryRepo libraryRepo;
        private readonly IPlaylistsRepo playlistRepo;
        private readonly IUpdateLibraryService updateLibraryService;
        private readonly Timer timer;

        private bool isCheckingForUpdates;
        private int libraryUpdateCount;

        public AutoUpdateLibraryService(ILibraryRepo libraryRepo, IPlaylistsRepo playlistRepo,
            IUpdateLibraryService updateLibraryService)
        {
            this.libraryRepo = libraryRepo;
            this.playlistRepo = playlistRepo;
            this.updateLibraryService = updateLibraryService;

            timer = new Timer(OnTick, null, Timeout.Infinite, timerInterval);

            isCheckingForUpdates = false;
            libraryUpdateCount = 0;
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
            if (isCheckingForUpdates || libraryUpdateCount > 0) return;

            try
            {
                isCheckingForUpdates = true;

                Library library = await libraryRepo.GetLibrary();
                if (NeedsFoldersUpdate(library.FoldersLastUpdated))
                {
                    await updateLibraryService.UpdatePlaylists();
                }

                library = await libraryRepo.GetLibrary();
                foreach (PlaylistInfo playlistInfo in library.Playlists.GetSourcePlaylists())
                {
                    if (NeedsSongsUpdate(playlistInfo.SongsLastUpdated))
                    {
                        await updateLibraryService.ReloadSourcePlaylist(playlistInfo.Id);
                    }
                    else if (NeedsFilesUpdate(playlistInfo.FilesLastUpdated))
                    {
                        await updateLibraryService.UpdateSourcePlaylist(playlistInfo.Id);
                    }
                }
            }
            finally
            {
                isCheckingForUpdates = false;
            }
        }

        public Task Start()
        {
            updateLibraryService.UpdateStarted += UpdateLibraryService_UpdateStarted;
            updateLibraryService.UpdateCompleted += UpdateLibraryService_UpdateCompleted;

            timer.Change(0, timerInterval);

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            updateLibraryService.UpdateStarted -= UpdateLibraryService_UpdateStarted;
            updateLibraryService.UpdateCompleted -= UpdateLibraryService_UpdateCompleted;

            timer.Change(Timeout.Infinite, timerInterval);

            return Task.CompletedTask;
        }

        private void UpdateLibraryService_UpdateStarted(object sender, EventArgs e)
        {
            libraryUpdateCount++;
        }

        private void UpdateLibraryService_UpdateCompleted(object sender, EventArgs e)
        {
            libraryUpdateCount--;
        }

        public async Task Dispose()
        {
            await Stop();
            timer.Dispose();
        }
    }
}
