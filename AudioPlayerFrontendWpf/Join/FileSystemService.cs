using AudioPlayerBackend.Audio;
using AudioPlayerBackend.FileSystem;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerFrontend.Join
{
    internal class FileSystemService : IFileSystemService
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

        public async Task UpdateSourcePlaylist(ISourcePlaylist playlist)
        {
            List<Song> songs = playlist. Songs.ToList();
            playlist.Songs = await Task.Run(async () =>
            {
                IEnumerable<string> allFiles = await FetchFiles(playlist.FileMediaSources);
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

        public async Task ReloadSourcePlaylist(ISourcePlaylist playlist)
        {
            List<Song> songs = playlist.Songs.ToList();
            playlist.Songs = await Task.Run(async () =>
            {
                IEnumerable<string> allFiles = await FetchFiles(playlist.FileMediaSources);
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

        private static Task<IEnumerable<string>> FetchFiles(string[] sources)
        {
            return Task.FromResult(LoadAllSongs(sources));
        }

        private static IEnumerable<string> LoadAllSongs(string[] sources)
        {
            try
            {
                return sources.ToNotNull().SelectMany(LoadFilePaths).ToArray();
            }
            catch
            {
                return Enumerable.Empty<string>();
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
    }
}
