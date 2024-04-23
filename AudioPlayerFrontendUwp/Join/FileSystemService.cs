using AudioPlayerBackend.Audio;
using AudioPlayerBackend.FileSystem;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace AudioPlayerFrontend.Join
{
    class FileSystemService : IFileSystemService, IEqualityComparer<StorageFile>
    {
        private static readonly Random ran = new Random();

        private readonly SemaphoreSlim loadMusicPropsSem;

        public FileSystemService()
        {
            loadMusicPropsSem = new SemaphoreSlim(10);
        }

        public async Task<string> ReadTextFile(string fileName)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
            return await FileIO.ReadTextAsync(file);
        }

        public async Task WriteTextFile(string fileName, string text)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(file, text);
        }

        public async Task UpdateSourcePlaylist(ISourcePlaylist playlist)
        {
            List<Song> songs = playlist.Songs.ToList();
            playlist.Songs = await Task.Run(async () =>
            {
                IEnumerable<StorageFile> allFiles = await LoadAllFiles(playlist.FileMediaSources);
                Dictionary<string, StorageFile> dict = allFiles.Distinct(this).ToDictionary(f => f.Path);

                for (int i = songs.Count - 1; i >= 0; i--)
                {
                    if (dict.ContainsKey(songs[i].FullPath)) dict.Remove(songs[i].FullPath);
                    else songs.RemoveAt(i);
                }

                Song?[] newSongs = await Task.WhenAll(dict.Values.Select(CreateSong));
                foreach (Song song in newSongs.OfType<Song>())
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
                IEnumerable<StorageFile> allFiles = await LoadAllFiles(playlist.FileMediaSources);
                IEnumerable<Song?> allSongs = await Task.WhenAll(allFiles.Distinct(this).Select(CreateSong));
                Dictionary<string, Song> loadedSongs = allSongs.OfType<Song>().ToDictionary(s => s.FullPath);

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

        private static async Task<IEnumerable<StorageFile>> LoadAllFiles(string[] sources)
        {
            try
            {
                List<StorageFile> files = new List<StorageFile>();
                foreach (string path in sources.ToNotNull())
                {
                    await AddLoadedFilePaths(files, path);
                }

                return files;
            }
            catch
            {
                return Enumerable.Empty<StorageFile>();
            }
        }

        private static async Task AddLoadedFilePaths(List<StorageFile> files, string path)
        {
            if (File.Exists(path)) files.Add(await StorageFile.GetFileFromPathAsync(path));
            else if (Directory.Exists(path))
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                files.AddRange(await folder.GetFilesAsync());
            }
        }

        public async Task<Song?> CreateSong(StorageFile file)
        {
            try
            {
                await loadMusicPropsSem.WaitAsync();
                MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

                if (properties.Duration == TimeSpan.Zero) return null;
                return new Song(file.Path)
                {
                    Title = string.IsNullOrWhiteSpace(properties.Title) ? file.Name : properties.Title,
                    Artist = string.IsNullOrWhiteSpace(properties.Artist) ? null : properties.Artist,
                };
            }
            catch
            {
                return null;
            }
            finally
            {
                loadMusicPropsSem.Release();
            }
        }

        public bool Equals(StorageFile x, StorageFile y)
        {
            return x.Path == y.Path;
        }

        public int GetHashCode(StorageFile obj)
        {
            return obj.Path.GetHashCode();
        }
    }
}
