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
        private readonly IInvokeDispatcherService dispatcherService;
        private readonly Timer timer;

        private bool isCheckingForUpdates = false;

        public AutoUpdateLibraryService(ILibraryRepo libraryRepo, IPlaylistsRepo playlistRepo,
            IUpdateLibraryService updateLibraryService, IInvokeDispatcherService dispatcherService)
        {
            this.libraryRepo = libraryRepo;
            this.playlistRepo = playlistRepo;
            this.updateLibraryService = updateLibraryService;
            this.dispatcherService = dispatcherService;

            timer = new Timer(OnTick, null, Timeout.Infinite, timerInterval);
        }

        private async void OnTick(object sender)
        {
            await CheckNeededUpdates();
        }

        private async Task InvokeDispatcher(Func<Task> func)
        {
            await await dispatcherService.InvokeDispatcher(func);
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
            if (isCheckingForUpdates) return;

            try
            {
                isCheckingForUpdates = true;

                Library library = await libraryRepo.GetLibrary();
                if (NeedsFoldersUpdate(library.FoldersLastUpdated))
                {
                    await InvokeDispatcher(() => updateLibraryService.UpdateLibrary());
                }

                library = await libraryRepo.GetLibrary();
                foreach (PlaylistInfo playlistInfo in library.Playlists.GetSourcePlaylists())
                {
                    if (NeedsSongsUpdate(playlistInfo.SongsLastUpdated))
                    {
                        await InvokeDispatcher(() => updateLibraryService.ReloadSourcePlaylist(playlistInfo.Id));
                    }
                    else if (NeedsFilesUpdate(playlistInfo.FilesLastUpdated))
                    {
                        await InvokeDispatcher(() => updateLibraryService.UpdateSourcePlaylist(playlistInfo.Id));
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
            timer.Change(0, timerInterval);

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            timer.Change(Timeout.Infinite, timerInterval);

            return Task.CompletedTask;
        }

        public async Task Dispose()
        {
            await Stop();
            timer.Dispose();
        }
    }
}
