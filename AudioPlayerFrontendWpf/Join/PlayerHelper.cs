using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using StdOttStandard.Linq;
using System.IO;

namespace AudioPlayerFrontend.Join
{
    class PlayerHelper : IAudioServicePlayerHelper, IAudioStreamHelper
    {
        private static readonly Random ran = new Random();
        private static PlayerHelper instance;

        public static PlayerHelper Current
        {
            get
            {
                if (instance == null) instance = new PlayerHelper();

                return instance;
            }
        }

        private PlayerHelper() { }

        public Action<Action> InvokeDispatcher => null;

        public void Reload(ISourcePlaylistBase playlist)
        {
            Song[] allSongsShuffled = GetShuffledSongs(LoadAllSongs(playlist.FileMediaSources)).ToArray();

            playlist.Songs = allSongsShuffled;
        }

        public void Update(ISourcePlaylistBase playlist)
        {
            Dictionary<string, Song> loadedSongs = LoadAllSongs(playlist.FileMediaSources).ToDictionary(s => s.FullPath);
            List<Song> songs = playlist.Songs.ToList();

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

            playlist.Songs = songs.ToArray();
        }

        private IEnumerable<Song> LoadAllSongs(string[] sources)
        {
            try
            {
                IEnumerable<string> sourcePaths = sources.ToNotNull();
                IEnumerable<string> nonHiddenFiles = sourcePaths.SelectMany(LoadFilePaths);

                return nonHiddenFiles.Select(p => new Song(p));
            }
            catch
            {
                return Enumerable.Empty<Song>();
            }
        }

        private IEnumerable<string> LoadFilePaths(string path)
        {
            if (File.Exists(path)) yield return path;

            if (Directory.Exists(path))
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

        private IEnumerable<Song> GetShuffledSongs(IEnumerable<Song> songs)
        {
            return songs.OrderBy(s => ran.Next());
        }
    }
}
