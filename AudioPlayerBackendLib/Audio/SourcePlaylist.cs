using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Audio
{
    class SourcePlaylist : Playlist, ISourcePlaylist
    {
        private static readonly Random ran = new Random();

        private readonly ISourcePlaylistHelper helper;
        private string[] fileMediaSources;

        public event EventHandler<ValueChangedEventArgs<string[]>> FileMediaSourcesChanged;

        public string[] FileMediaSources
        {
            get => fileMediaSources;
            set
            {
                if (value == fileMediaSources) return;

                var args = new ValueChangedEventArgs<string[]>(FileMediaSources, value);
                fileMediaSources = value;
                FileMediaSourcesChanged?.Invoke(this, args);

                OnPropertyChanged(nameof(FileMediaSources));
            }
        }

        public SourcePlaylist(Guid id, ISourcePlaylistHelper helper = null) : base(id, helper)
        {
            this.helper = helper;
        }

        public async Task Update()
        {
            IEnumerable<string> allFiles = await (helper?.FetchFiles ?? FetchFiles)(FileMediaSources);
            Dictionary<string, string> dict = allFiles.Distinct().ToDictionary(f => f);
            List<Song> songs = Songs.ToList();

            for (int i = songs.Count - 1; i >= 0; i--)
            {
                if (dict.ContainsKey(songs[i].FullPath)) dict.Remove(songs[i].FullPath);
                else songs.RemoveAt(i);
            }

            foreach (Task<Song?> task in dict.Keys.Select(helper?.CreateSong ?? CreateSong))
            {
                Song? song = await task;
                if (song.HasValue) songs.Insert(ran.Next(songs.Count + 1), song.Value);
            }

            Songs = songs.ToArray();
        }

        public async Task Reload()
        {
            IEnumerable<string> allFiles = await (helper?.FetchFiles ?? FetchFiles)(FileMediaSources);
            IEnumerable<Song?> allSongs = await Task.WhenAll(allFiles.Distinct().Select(helper?.CreateSong ?? CreateSong));
            Dictionary<string, Song> loadedSongs = allSongs.Where(s => s.HasValue)
                .Select(s => s.Value).Distinct().ToDictionary(s => s.FullPath);
            List<Song> songs = Songs.ToList();

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

            Songs = songs.ToArray();
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

        private static Task<Song?> CreateSong(string path)
        {
            return Task.FromResult<Song?>(new Song(path));
        }
    }
}
