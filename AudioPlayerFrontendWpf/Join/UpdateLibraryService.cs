using AudioPlayerBackend.AudioLibrary.PlaylistRepo.Extensions;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.FileSystem;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary;

namespace AudioPlayerFrontend.Join
{
    internal class UpdateLibraryService : IUpdateLibraryService
    {
        private static readonly Random ran = new Random();
        private readonly IServicedLibraryRepo libraryRepo;
        private readonly IServicedPlaylistsRepo playlistsRepo;

        public UpdateLibraryService(IServicedLibraryRepo libraryRepo, IServicedPlaylistsRepo playlistsRepo)
        {
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;
        }

        public async Task UpdateLibrary()
        {
            await UpdateReloadLibrary(false);
        }

        public async Task ReloadLibrary()
        {
            await UpdateReloadLibrary(true);
        }

        private async Task UpdateReloadLibrary(bool reload)
        {
            Library library = await libraryRepo.GetLibrary();

            foreach (PlaylistInfo playlist in library.Playlists.GetSourcePlaylists())
            {
                await (reload ? ReloadSourcePlaylist(playlist.Id) : UpdateSourcePlaylist(playlist.Id));
                await UpdateFolders(playlist.Id);
            }

            await libraryRepo.SendFoldersLastUpdatedChange(DateTime.Now);
        }

        private async Task UpdateFolders(Guid playlistId)
        {
            Playlist playlist = await playlistsRepo.GetPlaylist(playlistId);
            FileMediaSources fileMediaSources = playlist.FileMediaSources;
            if (fileMediaSources == null
                || !fileMediaSources.Root.UpdateType.HasFlag(FileMediaSourceRootUpdateType.Folders)) return;

            FileMediaSourceRoot root = fileMediaSources.Root;
            ICollection<FileMediaSource> allSources = await playlistsRepo.GetFileMediaSourcesOfRoot(root.Id);

            foreach (FileMediaSource source in fileMediaSources.Sources)
            {
                string path = GetFileMediaSourcePath(source, root);
                if (!Directory.Exists(path)) continue;

                await CheckFolders(root.Path.Length, path);
            }

            async Task CheckFolders(int rootLength, string folderPath)
            {
                foreach (string subFolderPath in Directory.GetDirectories(folderPath))
                {
                    string relativePath = FileMediaSource.NormalizeRelativePath(subFolderPath.Substring(rootLength));
                    if (allSources.Any(s => s.RelativePath == relativePath)) return;

                    await TryCreatePlaylist(root, relativePath);
                    await CheckFolders(rootLength, subFolderPath);
                }
            }
        }

        private async Task TryCreatePlaylist(FileMediaSourceRoot root, string relativePath)
        {
            FileMediaSources fileMediaSources = new FileMediaSources(root, new FileMediaSource[]
            {
                new FileMediaSource(relativePath),
            });

            Song[] songs = await ReloadSourcePlaylist(fileMediaSources);
            if (songs.Length == 0) return;

            PlaylistType playlistType = PlaylistType.SourcePlaylist | PlaylistType.AutoSourcePlaylist;
            string name = Path.GetFileName(relativePath);
            Playlist playlist = new Playlist(Guid.NewGuid(), playlistType, name,
                OrderType.ByTitleAndArtist, LoopType.CurrentPlaylist, 1,
                TimeSpan.Zero, TimeSpan.Zero, null, null, songs, fileMediaSources,
                null, DateTime.Now, DateTime.Now);

            await playlistsRepo.SendInsertPlaylist(playlist, null);
        }

        public async Task UpdateSourcePlaylist(Guid id)
        {
            Playlist playlist = await playlistsRepo.GetPlaylist(id);
            FileMediaSources fileMediaSources = playlist.FileMediaSources;
            if (fileMediaSources == null
                || !fileMediaSources.Root.UpdateType.HasFlag(FileMediaSourceRootUpdateType.Songs)) return;

            List<Song> oldSongs = playlist.Songs?.ToList() ?? new List<Song>();
            Song[] newSongs = await Task.Run(() =>
            {
                IEnumerable<string> allFiles = LoadAllFilePaths(playlist.FileMediaSources);
                Dictionary<string, string> dict = allFiles.Distinct().ToDictionary(f => f);

                for (int i = oldSongs.Count - 1; i >= 0; i--)
                {
                    if (dict.ContainsKey(oldSongs[i].FullPath)) dict.Remove(oldSongs[i].FullPath);
                    else oldSongs.RemoveAt(i);
                }

                foreach (Song song in dict.Keys.Select(CreateSong))
                {
                    oldSongs.Insert(ran.Next(oldSongs.Count + 1), song);
                }

                return oldSongs.ToArray();
            });

            if (newSongs.Length > 0)
            {
                await playlistsRepo.SendSongsChange(id, newSongs);
                await playlistsRepo.SendFilesLastUpdatedChange(id, DateTime.Now);
            }
            else await playlistsRepo.SendRemovePlaylist(id);
        }

        public async Task ReloadSourcePlaylist(Guid id)
        {
            Playlist playlist = await playlistsRepo.GetPlaylist(id);
            Song[] newSongs = await ReloadSourcePlaylist(playlist.FileMediaSources, playlist.Songs);

            if (newSongs.Length > 0)
            {
                await playlistsRepo.SendSongsChange(id, newSongs);
                await playlistsRepo.SendSongsLastUpdatedChange(id, DateTime.Now);
            }
            else await playlistsRepo.SendRemovePlaylist(id);
        }

        public Task<Song[]> ReloadSourcePlaylist(FileMediaSources fileMediaSources)
        {
            return ReloadSourcePlaylist(fileMediaSources, null);
        }

        private Task<Song[]> ReloadSourcePlaylist(FileMediaSources fileMediaSources, IEnumerable<Song> oldSongs)
        {
            List<Song> songs = oldSongs?.ToList() ?? new List<Song>();
            return Task.Run(() =>
            {
                IEnumerable<string> allFiles = LoadAllFilePaths(fileMediaSources);
                IEnumerable<Song> allSongs = allFiles.Distinct().Select(CreateSong);
                Dictionary<string, Song> loadedSongs = allSongs.ToDictionary(s => s.FullPath);

                for (int i = songs.Count - 1; i >= 0; i--)
                {
                    Song loadedSong;
                    if (loadedSongs.TryGetValue(songs[i].FullPath, out loadedSong))
                    {
                        songs[i] = loadedSong;
                        loadedSongs.Remove(songs[i].FullPath);
                    }
                    else songs.RemoveAt(i);
                }

                foreach (Song song in loadedSongs.Values)
                {
                    songs.Insert(ran.Next(songs.Count + 1), song);
                }

                return songs.ToArray();
            });
        }

        public static IEnumerable<string> LoadAllFilePaths(FileMediaSources sources)
        {
            try
            {
                return sources.Sources
                    .Select(s => GetFileMediaSourcePath(s, sources.Root))
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .SelectMany(LoadFilePaths).ToArray();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        private static string GetFileMediaSourcePath(FileMediaSource source, FileMediaSourceRoot root)
        {
            string rootPath = GetPathFromFileMediaSourceRoot(root);
            if (string.IsNullOrWhiteSpace(rootPath)) return null;

            return Path.Combine(rootPath, source.RelativePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string GetPathFromFileMediaSourceRoot(FileMediaSourceRoot root)
        {
            switch (root.PathType)
            {
                case FileMediaSourceRootPathType.Path:
                    return root.Path;

                case FileMediaSourceRootPathType.KnownFolder:
                    return GetLocalKnownFolders()
                        .TryFirst(f => f.Value == root.Path, out LocalKnownFolder folder)
                        ? folder.CurrentFullPath : null;

                default:
                    return null;
            }
        }

        private static IEnumerable<string> LoadFilePaths(string path)
        {
            if (File.Exists(path)) yield return path;
            else if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    if (IsNotHidden(file)) yield return file;
                }
            }
        }

        private static bool IsNotHidden(string path)
        {
            FileInfo file = new FileInfo(path);

            return (file.Attributes & FileAttributes.Hidden) == 0;
        }

        private static Song CreateSong(string path)
        {
            return new Song(path);
        }

        public static IEnumerable<LocalKnownFolder> GetLocalKnownFolders()
        {
            yield return CreateLocalKnownFolder(Environment.SpecialFolder.MyMusic, "My Music");
            yield return CreateLocalKnownFolder(Environment.SpecialFolder.MyVideos, "My Videos");
            yield return CreateLocalKnownFolder(Environment.SpecialFolder.MyPictures, "My Pictures");
            yield return CreateLocalKnownFolder(Environment.SpecialFolder.MyDocuments, "My Documents");
            yield return CreateLocalKnownFolder(Environment.SpecialFolder.DesktopDirectory, "Desktop");
        }

        private static LocalKnownFolder CreateLocalKnownFolder(Environment.SpecialFolder folder, string name)
        {
            string value = folder.ToString();
            string fullPath = Environment.GetFolderPath(folder);

            return new LocalKnownFolder(name, value, fullPath);
        }
    }
}
