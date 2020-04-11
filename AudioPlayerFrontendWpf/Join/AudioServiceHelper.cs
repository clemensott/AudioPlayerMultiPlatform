using AudioPlayerBackend.Audio;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AudioPlayerFrontend.Join
{
    class AudioServiceHelper : IAudioServiceHelper
    {
        private static readonly Random ran = new Random();
        private static AudioServiceHelper instance;

        public static AudioServiceHelper Current
        {
            get
            {
                if (instance == null) instance = new AudioServiceHelper();

                return instance;
            }
        }

        private AudioServiceHelper() { }

        public Action<Action> InvokeDispatcher => null;

        public void Reload(ISourcePlaylist playlist)
        {
            Song[] allSongsShuffled = GetShuffledSongs(LoadAllSongs(playlist.FileMediaSources)).ToArray();

            for (int i = 0; i < allSongsShuffled.Length; i++) allSongsShuffled[i].Index = i;

            playlist.Songs = allSongsShuffled;
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
