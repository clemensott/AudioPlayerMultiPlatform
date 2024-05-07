using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Audio.MediaSource;
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
        private static readonly char[] diretorySeparators = new char[] { '/', '\\' };
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

        public async Task UpdateSourcePlaylist(ISourcePlaylist playlist, FileMediaSourceRoot[] roots)
        {
            List<Song> songs = playlist.Songs.ToList();
            playlist.Songs = await Task.Run(async () =>
            {
                IEnumerable<StorageFile> allFiles = await LoadAllFiles(playlist.FileMediaSources, roots);
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

        public async Task ReloadSourcePlaylist(ISourcePlaylist playlist, FileMediaSourceRoot[] roots)
        {
            List<Song> songs = playlist.Songs.ToList();
            playlist.Songs = await Task.Run(async () =>
            {
                IEnumerable<StorageFile> allFiles = await LoadAllFiles(playlist.FileMediaSources, roots);
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

        private static async Task<IEnumerable<StorageFile>> LoadAllFiles(FileMediaSource[] sources, FileMediaSourceRoot[] roots)
        {
            try
            {
                List<StorageFile> files = new List<StorageFile>();
                foreach (FileMediaSource source in sources.ToNotNull())
                {
                    if (roots.TryFirst(r => r.Id == source.RootId, out FileMediaSourceRoot root))
                    {
                        StorageFolder rootFolder = await LoadFileMediaSourceRootFolder(root);
                        IStorageItem sourceStorageItem = await LoadFileMediaSourceStorageItem(source, rootFolder);
                        await AddLoadedFilePaths(files, sourceStorageItem);
                    }
                }

                return files;
            }
            catch
            {
                return Enumerable.Empty<StorageFile>();
            }
        }

        private static async Task<IStorageItem> LoadFileMediaSourceStorageItem(FileMediaSource source, StorageFolder rootFolder)
        {
            IStorageItem lastItem = rootFolder;
            if (string.IsNullOrWhiteSpace(source.RelativePath)) return lastItem;
            foreach (string subName in source.RelativePath.Split(diretorySeparators))
            {
                if (lastItem is StorageFolder folder)
                {
                    lastItem = await folder.TryGetItemAsync(subName);
                    if (lastItem == null) return null;
                }
                else return null;
            }

            return lastItem;
        }

        private static async Task AddLoadedFilePaths(List<StorageFile> files, IStorageItem source)
        {
            if (source is StorageFile file) files.Add(file);
            else if (source is StorageFolder folder) files.AddRange(await folder.GetFilesAsync());
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

        private static async Task<StorageFolder> LoadFileMediaSourceRootFolder(FileMediaSourceRoot root)
        {
            if (root.Type == FileMediaSourceRootType.Path) return await StorageFolder.GetFolderFromPathAsync(root.Value);

            return GetLocalKnownFolderIds().First(folder => folder.Name == root.Value);
        }

        private static IEnumerable<StorageFolder> GetLocalKnownFolderIds()
        {
            yield return KnownFolders.MusicLibrary;
            yield return KnownFolders.Playlists;
            yield return KnownFolders.RecordedCalls;
            yield return KnownFolders.VideosLibrary;
            yield return KnownFolders.CameraRoll;
            yield return KnownFolders.PicturesLibrary;
            yield return KnownFolders.DocumentsLibrary;
            yield return KnownFolders.RemovableDevices;
            yield return KnownFolders.MediaServerDevices;
            yield return KnownFolders.HomeGroup;
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
