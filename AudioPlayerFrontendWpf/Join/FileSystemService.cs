using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.FileSystem;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerFrontend.Join
{
    class FileSystemService : IFileSystemService
    {
        private static readonly Random ran = new Random();
        private readonly IServicedPlaylistsRepo playlistsRepo;

        public FileSystemService(IServicedPlaylistsRepo playlistsRepo)
        {
            this.playlistsRepo = playlistsRepo;
        }

        public Task<string> ReadTextFile(string fileName)
        {
            return Task.FromResult(File.ReadAllText(fileName));
        }

        public Task WriteTextFile(string fileName, string text)
        {
            File.WriteAllText(fileName, text);
            return Task.CompletedTask;
        }

        public Task UpdateLibrary()
        {
            return ReloadLibrary();
        }

        public Task ReloadLibrary()
        {
            throw new NotImplementedException();
        }

        public async Task UpdateSourcePlaylist(Guid id)
        {
            Playlist playlist = await playlistsRepo.GetPlaylist(id);
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

            await playlistsRepo.SendSongsChange(id, newSongs);
        }

        public async Task ReloadSourcePlaylist(Guid id)
        {
            Playlist playlist = await playlistsRepo.GetPlaylist(id);
            Song[] newSongs = await ReloadSourcePlaylist(playlist.FileMediaSources);

            await playlistsRepo.SendSongsChange(id, newSongs);
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

        private IEnumerable<string> LoadAllFilePaths(FileMediaSources sources)
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

        private string GetFileMediaSourcePath(FileMediaSource source, FileMediaSourceRoot root)
        {
            string rootPath = GetPathFromFileMediaSourceRoot(root);
            if (string.IsNullOrWhiteSpace(rootPath)) return null;

            return Path.Combine(rootPath, source.RelativePath);
        }

        private string GetPathFromFileMediaSourceRoot(FileMediaSourceRoot root)
        {
            switch (root.Type)
            {
                case FileMediaSourceRootType.Path:
                    return root.Path;

                case FileMediaSourceRootType.KnownFolder:
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

        public IEnumerable<LocalKnownFolder> GetLocalKnownFolders()
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
