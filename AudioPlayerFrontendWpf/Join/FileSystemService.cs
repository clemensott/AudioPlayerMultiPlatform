using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Audio.MediaSource;
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

        public Task<string> ReadTextFile(string fileName)
        {
            return Task.FromResult(File.ReadAllText(fileName));
        }

        public Task WriteTextFile(string fileName, string text)
        {
            File.WriteAllText(fileName, text);
            return Task.CompletedTask;
        }

        public async Task UpdateSourcePlaylist(ISourcePlaylist playlist, FileMediaSourceRoot[] roots)
        {
            List<Song> songs = playlist.Songs.ToList();
            playlist.Songs = await Task.Run(() =>
            {
                IEnumerable<string> allFiles = LoadAllFilePaths(playlist.FileMediaSources, roots);
                Dictionary<string, string> dict = allFiles.Distinct().ToDictionary(f => f);

                for (int i = songs.Count - 1; i >= 0; i--)
                {
                    if (dict.ContainsKey(songs[i].FullPath)) dict.Remove(songs[i].FullPath);
                    else songs.RemoveAt(i);
                }

                foreach (Song song in dict.Keys.Select(CreateSong))
                {
                    songs.Insert(ran.Next(songs.Count + 1), song);
                }

                return songs.ToArray();
            });
        }

        public async Task ReloadSourcePlaylist(ISourcePlaylist playlist, FileMediaSourceRoot[] roots)
        {
            List<Song> songs = playlist.Songs.ToList();
            playlist.Songs = await Task.Run(() =>
            {
                IEnumerable<string> allFiles = LoadAllFilePaths(playlist.FileMediaSources, roots);
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

        private IEnumerable<string> LoadAllFilePaths(FileMediaSource[] sources, FileMediaSourceRoot[] roots)
        {
            try
            {
                return sources.ToNotNull()
                    .Select(s => GetFileMediaSourcePath(s, roots))
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .SelectMany(LoadFilePaths).ToArray();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        private string GetFileMediaSourcePath(FileMediaSource source, FileMediaSourceRoot[] roots)
        {
            if (!roots.ToNotNull().TryFirst(r => r.Id == source.RootId, out FileMediaSourceRoot root)) return null;

            string rootPath = GetPathFromFileMediaSourceRoot(root);
            if (string.IsNullOrWhiteSpace(rootPath)) return null;

            return Path.Combine(rootPath, source.RelativePath);
        }

        private string GetPathFromFileMediaSourceRoot(FileMediaSourceRoot root)
        {
            switch (root.Type)
            {
                case FileMediaSourceRootType.Path:
                    return root.Value;

                case FileMediaSourceRootType.KnownFolder:
                    return GetLocalKnownFolders()
                        .TryFirst(f => f.Value == root.Value, out LocalKnownFolder folder)
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
