using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.FileSystem
{
    internal class UpdateLibraryService : IAudioService
    {
        private readonly ILibraryRepo libraryRepo;
        private readonly IPlaylistsRepo playlistsRepo;
        private readonly IFileSystemService fileSystemService;

        public UpdateLibraryService(ILibraryRepo libraryRepo, IPlaylistsRepo playlistsRepo, IFileSystemService fileSystemService)
        {
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
            this.fileSystemService = fileSystemService;
        }

        /// <summary>
        /// Checks file system for media files of a playlist.
        /// </summary>
        /// <param name="id">ID of Playlist</param>
        /// <returns></returns>
        public async Task UpdatePlaylistFiles(Guid id)
        {

        }

        /// <summary>
        /// Checks file system for media files and updates all songs of a playlist.
        /// </summary>
        /// <returns></returns>
        public async Task UpdatePlaylistFilesAndSongs(Guid id)
        {

        }

        /// <summary>
        /// Checks file system for playlists and media files of library.
        /// </summary>
        /// <returns></returns>
        public async Task UpdateLibraryFiles()
        {

        }

        /// <summary>
        /// Checks file system for playlists and media files and updates all songs of library.
        /// </summary>
        /// <param name="id">ID of Playlist</param>
        /// <returns></returns>
        public async Task UpdateLibraryFilesAndSongs()
        {

        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public Task Dispose()
        {
            return Task.CompletedTask;
        }
    }
}
