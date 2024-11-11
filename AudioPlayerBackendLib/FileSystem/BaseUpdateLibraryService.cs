using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.Extensions;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Build;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.FileSystem
{
    public abstract class BaseUpdateLibraryService<TFile> : IUpdateLibraryService
    {
        private static readonly Random ran = new Random();

        private readonly SongEqualityComparer songEqualityComparer;
        private readonly FileMediaSourceRootInfo[] defaultUpdateRoots;
        private readonly ILibraryRepo libraryRepo;
        private readonly IPlaylistsRepo playlistsRepo;

        private readonly SemaphoreSlim loadMusicPropsSem;

        public event EventHandler UpdateStarted;
        public event EventHandler UpdateCompleted;

        public BaseUpdateLibraryService(AudioServicesBuildConfig config, ILibraryRepo libraryRepo, IPlaylistsRepo playlistsRepo)
        {
            songEqualityComparer = new SongEqualityComparer();
            defaultUpdateRoots = config.DefaultUpdateRoots;
            this.libraryRepo = libraryRepo;
            this.playlistsRepo = playlistsRepo;

            loadMusicPropsSem = new SemaphoreSlim(10);
        }

        private async Task RunWithEvents(Func<Task> action)
        {
            try
            {
                UpdateStarted?.Invoke(this, EventArgs.Empty);
                await action();
            }
            finally
            {
                UpdateCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task UpdateLibrary()
        {
            await UpdateReloadLibrary(false);
        }

        public async Task ReloadLibrary()
        {
            await UpdateReloadLibrary(true);
        }

        private Task UpdateReloadLibrary(bool reload)
        {
            return RunWithEvents(async () =>
            {
                await UpdatePlaylistsInternal();

                Library library = await libraryRepo.GetLibrary();
                foreach (PlaylistInfo playlist in library.Playlists.GetSourcePlaylists())
                {
                    await (reload ? ReloadSourcePlaylistInternal(playlist.Id) : UpdateSourcePlaylistInternal(playlist.Id));
                }

                await libraryRepo.SetFoldersLastUpdated(DateTime.Now);
            });
        }

        public Task UpdatePlaylists()
        {
            return RunWithEvents(async () =>
            {
                await UpdatePlaylistsInternal();
                await libraryRepo.SetFoldersLastUpdated(DateTime.Now);
            });
        }

        public async Task UpdatePlaylistsInternal()
        {
            FileMediaSourceRootInfo[] defaultUpdateRoots = this.defaultUpdateRoots.ToNotNull().ToArray();
            ICollection<FileMediaSourceRoot> roots = await playlistsRepo.GetFileMediaSourceRoots();

            foreach (FileMediaSourceRoot root in roots)
            {
                await CheckRootForNewPlaylists(root);

                defaultUpdateRoots = defaultUpdateRoots.Where(r => !EqualRoot(r, root)).ToArray();
            }

            foreach (FileMediaSourceRootInfo defaultRoot in defaultUpdateRoots)
            {
                await CheckRootForNewPlaylists(defaultRoot.CreateRoot());
            }
        }

        private async Task CheckRootForNewPlaylists(FileMediaSourceRoot root)
        {
            ICollection<FileMediaSource> allSources = await playlistsRepo.GetFileMediaSourcesOfRoot(root.Id);
            bool withSubFolders = root.UpdateType.HasFlag(FileMediaSourceRootUpdateType.Folders);
            await CheckRootForNewPlaylists(allSources, root, withSubFolders);
        }

        private static bool EqualRoot(FileMediaSourceRootInfo defaultRoot, FileMediaSourceRoot root)
        {
            return defaultRoot.UpdateType == root.UpdateType
                && defaultRoot.PathType == root.PathType
                && defaultRoot.Path == root.Path;
        }

        protected abstract Task CheckRootForNewPlaylists(ICollection<FileMediaSource> allSources, FileMediaSourceRoot root, bool withSubFolders);

        protected async Task TryCreatePlaylist(FileMediaSourceRoot root, string relativePath)
        {
            FileMediaSources fileMediaSources = new FileMediaSources(root, new FileMediaSource[]
            {
                new FileMediaSource(relativePath),
            });

            Song[] songs = await ReloadSourcePlaylist(fileMediaSources);
            if (songs.Length == 0) return;

            PlaylistType playlistType = PlaylistType.SourcePlaylist | PlaylistType.AutoSourcePlaylist;
            string name = string.IsNullOrWhiteSpace(relativePath) ? root.Name : Path.GetFileName(relativePath);
            Playlist playlist = new Playlist(Guid.NewGuid(), playlistType, name,
                OrderType.ByTitleAndArtist, LoopType.CurrentPlaylist, 1,
                null, songs, fileMediaSources, null, DateTime.Now, DateTime.Now);

            await playlistsRepo.InsertPlaylist(playlist, null);
        }

        public Task UpdateSourcePlaylist(Guid id)
        {
            return RunWithEvents(() => UpdateSourcePlaylistInternal(id));
        }

        public async Task UpdateSourcePlaylistInternal(Guid id)
        {
            Playlist playlist = await playlistsRepo.GetPlaylist(id);
            FileMediaSources fileMediaSources = playlist.FileMediaSources;
            if (fileMediaSources == null
                || !fileMediaSources.Root.UpdateType.HasFlag(FileMediaSourceRootUpdateType.Songs)) return;

            List<Song> oldSongs = playlist.Songs?.ToList() ?? new List<Song>();
            Song[] newSongs = await Task.Run(async () =>
            {
                IEnumerable<TFile> allFiles = await LoadAllFiles(playlist.FileMediaSources);
                Dictionary<string, TFile> dict = allFiles.ToDictionary(GetFileFullPath);

                for (int i = oldSongs.Count - 1; i >= 0; i--)
                {
                    if (dict.ContainsKey(oldSongs[i].FullPath)) dict.Remove(oldSongs[i].FullPath);
                    else oldSongs.RemoveAt(i);
                }

                await Task.WhenAll(dict.Values.Select(async file =>
                {
                    Song? song = await CreateSongInternal(file);
                    if (song == null) return;

                    oldSongs.Insert(ran.Next(oldSongs.Count + 1), song.Value);
                }));

                return oldSongs.ToArray();
            });

            if (newSongs.Length > 0)
            {
                if (!newSongs.BothNullOrSequenceEqual(playlist.Songs, songEqualityComparer))
                {
                    await playlistsRepo.SetSongs(id, newSongs);
                }

                await playlistsRepo.SetFilesLastUpdated(id, DateTime.Now);
            }
            else await playlistsRepo.RemovePlaylist(id);
        }

        public Task ReloadSourcePlaylist(Guid id)
        {
            return RunWithEvents(() => ReloadSourcePlaylistInternal(id));
        }

        public async Task ReloadSourcePlaylistInternal(Guid id)
        {
            Playlist playlist = await playlistsRepo.GetPlaylist(id);
            Song[] newSongs = await ReloadSourcePlaylist(playlist.FileMediaSources, playlist.Songs);

            if (newSongs.Length > 0)
            {
                if (!newSongs.BothNullOrSequenceEqual(playlist.Songs, songEqualityComparer))
                {
                    await playlistsRepo.SetSongs(id, newSongs);
                }

                await playlistsRepo.SetSongsLastUpdated(id, DateTime.Now);
            }
            else await playlistsRepo.RemovePlaylist(id);
        }

        public Task<Song[]> ReloadSourcePlaylist(FileMediaSources fileMediaSources)
        {
            return ReloadSourcePlaylist(fileMediaSources, null);
        }

        private Task<Song[]> ReloadSourcePlaylist(FileMediaSources fileMediaSources, IEnumerable<Song> oldSongs)
        {
            List<Song> songs = oldSongs?.ToList() ?? new List<Song>();
            return Task.Run(async () =>
            {
                IEnumerable<TFile> allFiles = await LoadAllFiles(fileMediaSources);
                Song[] allSongs = (await Task.WhenAll(allFiles.Select(CreateSongInternal))).OfType<Song>().ToArray();
                Dictionary<string, Song> loadedSongs = allSongs.ToDictionary(s => s.FullPath);

                for (int i = songs.Count - 1; i >= 0; i--)
                {
                    Song loadedSong;
                    if (loadedSongs.TryGetValue(songs[i].FullPath, out loadedSong))
                    {
                        loadedSong.Id = songs[i].Id;
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

        protected abstract Task<IEnumerable<TFile>> LoadAllFiles(FileMediaSources sources);

        private async Task<Song?> CreateSongInternal(TFile file)
        {
            try
            {
                await loadMusicPropsSem.WaitAsync();

                return await CreateSong(file);
            }
            finally
            {
                loadMusicPropsSem.Release();
            }
        }

        protected abstract Task<Song?> CreateSong(TFile file);

        protected abstract string GetFileFullPath(TFile file);

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

        class SongEqualityComparer : IEqualityComparer<Song>
        {
            public bool Equals(Song x, Song y)
            {
                return x.Id == y.Id
                    && x.Artist == y.Artist
                    && x.Title == y.Title
                    && x.FullPath == y.FullPath;
            }

            public int GetHashCode(Song obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
