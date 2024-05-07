using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.FileSystem;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace AudioPlayerFrontend
{
    static class UpdateHelper
    {
        public static Task Reload(IAudioService service)
        {
            return Update(service, true);
        }

        public static Task Update(IAudioService service)
        {
            return Update(service, false);
        }

        public static async Task Update(IAudioService service, bool reload)
        {
            IAudioCreateService audioCreateService = AudioPlayerServiceProvider.Current.GetAudioCreateService();
            IFileSystemService fileSystemService = AudioPlayerServiceProvider.Current.GetFileSystemService();

            IEnumerable<StorageFolder> folders = await GetAllFolders(KnownFolders.MusicLibrary);
            List<ISourcePlaylist> updatedSourcePlaylists = new List<ISourcePlaylist>();

            int nextIndex = 0;
            foreach (StorageFolder folder in folders)
            {
                string[] paths;
                ISourcePlaylist playlist;

                if (string.IsNullOrWhiteSpace(folder.Path))
                {
                    IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

                    paths = files.Select(f => f.Path).Select(Path.GetDirectoryName).Distinct().ToArray();
                }
                else paths = new string[] { folder.Path };

                if (service.SourcePlaylists.TryFirst(p => HasSameSource(p, paths), out playlist))
                {
                    await (reload 
                        ? fileSystemService.ReloadSourcePlaylist(playlist, service.FileMediaSourceRoots)
                        : fileSystemService.UpdateSourcePlaylist(playlist, service.FileMediaSourceRoots));

                    if (playlist.Songs.Length == 0)
                    {
                        service.SourcePlaylists.Remove(playlist);
                        return;
                    }

                    int currentIndex;
                    if (service.SourcePlaylists.TryIndexOf(p => HasSameSource(p, paths), out currentIndex))
                    {
                        nextIndex = currentIndex + 1;
                    }
                }
                else
                {
                    playlist = audioCreateService.CreateSourcePlaylist(Guid.NewGuid());
                    playlist.Name = folder.DisplayName;
                    playlist.FileMediaSources = paths;
                    await fileSystemService.UpdateSourcePlaylist(playlist);

                    if (playlist.Songs.Length == 0) continue;

                    int index = Math.Max(service.SourcePlaylists.Count, nextIndex++);
                    service.SourcePlaylists.Insert(index, playlist);
                }

                updatedSourcePlaylists.Add(playlist);

                if (service.CurrentPlaylist == null) service.CurrentPlaylist = playlist;
            }

            await Task.WhenAll(service.SourcePlaylists.Except(updatedSourcePlaylists).Select(fileSystemService.UpdateSourcePlaylist));

            IDictionary<string, Song> allSongs = service.SourcePlaylists
                .SelectMany(p => p.Songs).Distinct().ToDictionary(s => s.FullPath);

            foreach (IPlaylist playlist in service.Playlists)
            {
                Song song = new Song();
                playlist.Songs = playlist.Songs
                    .Where(s => allSongs.TryGetValue(s.FullPath, out song)).Select(_ => song).ToArray();
            }

            if (!service.GetAllPlaylists().Contains(service.CurrentPlaylist))
            {
                service.CurrentPlaylist = service.GetAllPlaylists().FirstOrDefault();
            }
        }

        public static async Task UpdatePlaylists(IAudioService service)
        {
            IAudioCreateService audioCreateService = AudioPlayerServiceProvider.Current.GetAudioCreateService();
            IFileSystemService fileSystemService = AudioPlayerServiceProvider.Current.GetFileSystemService();

            IEnumerable<StorageFolder> folders = await GetAllFolders(KnownFolders.MusicLibrary);

            int nextIndex = 0;
            foreach (StorageFolder folder in folders)
            {
                string[] paths;
                if (string.IsNullOrWhiteSpace(folder.Path))
                {
                    IReadOnlyList<StorageFile> files = (await folder.GetFilesAsync()).ToArray();

                    paths = files.Select(f => f.Path).Select(Path.GetDirectoryName).Distinct().ToArray();
                }
                else paths = new string[] { folder.Path };

                if (paths.Length == 0) continue;

                int currentIndex;
                if (service.SourcePlaylists.TryIndexOf(p => HasSameSource(p, paths), out currentIndex))
                {
                    nextIndex = currentIndex + 1;
                    continue;
                }

                ISourcePlaylist playlist = audioCreateService.CreateSourcePlaylist(Guid.NewGuid());
                playlist.Name = folder.DisplayName;
                playlist.FileMediaSources = paths;
                await fileSystemService.UpdateSourcePlaylist(playlist);

                if (playlist.Songs.Length == 0) continue;

                int index = Math.Max(service.SourcePlaylists.Count, nextIndex++);
                service.SourcePlaylists.Insert(index, playlist);

                if (service.CurrentPlaylist == null) service.CurrentPlaylist = playlist;
            }
        }

        private static async Task<IEnumerable<StorageFolder>> GetAllFolders(StorageFolder folder)
        {
            List<StorageFolder> allFolders = new List<StorageFolder>();
            Queue<StorageFolder> queue = new Queue<StorageFolder>();
            queue.Enqueue(folder);

            while (queue.Count > 0)
            {
                StorageFolder currentFolder = queue.Dequeue();
                allFolders.Add(currentFolder);

                foreach (StorageFolder subFolder in await currentFolder.GetFoldersAsync())
                {
                    queue.Enqueue(subFolder);
                }
            }

            return allFolders;
        }

        private static bool HasSameSource(ISourcePlaylist playlist, string[] paths)
        {
            return playlist.FileMediaSources.BothNullOrSequenceEqual(paths);
        }
    }
}
