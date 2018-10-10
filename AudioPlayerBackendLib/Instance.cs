using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AudioPlayerBackendLib
{
    class Instance : IDisposable
    {
        private static Random ran = new Random();

        private Window window;

        public MediaElement Player { get; private set; }

        private bool isAllShuffle, isSearchShuffle;
        private string searchKey, lowerSearchKey;
        private string[] mediaSources;
        private Song currentSong;
        private Song[] allSongsShuffled, allSongs;


        public bool Inited { get; private set; }

        public bool IsAllShuffle
        {
            get { return isAllShuffle; }
            set
            {
                if (value == isAllShuffle) return;

                isAllShuffle = value;
                AllSongs = GetAllSongs(allSongsShuffled, isAllShuffle).ToArray();
            }
        }

        public bool IsSearchShuffle
        {
            get { return isSearchShuffle; }
            set
            {
                if (value == isSearchShuffle) return;

                isSearchShuffle = value;
                SearchSongs = GetSearchSongs(allSongsShuffled, IsSearchShuffle).ToArray();
            }
        }

        public bool IsOnlySearch { get; set; }

        public string SearchKey
        {
            get { return searchKey; }
            set
            {
                if (value == searchKey) return;

                searchKey = value;
                lowerSearchKey = searchKey.ToLower();

                SearchSongs = GetSearchSongs(allSongsShuffled, IsSearchShuffle).ToArray();
            }
        }

        public string[] MediaSources
        {
            get { return mediaSources; }
            set
            {
                if (value == mediaSources) return;

                mediaSources = value;
                Refresh();
            }
        }

        public Song CurrentSong
        {
            get { return currentSong; }
            set
            {
                if (value == currentSong) return;

                currentSong = value;
                Player.Source = currentSong.Uri;
            }
        }

        public Song[] AllSongs
        {
            get { return allSongs; }
            set
            {
                if (value == allSongs) return;

                allSongs = value;

                if (!allSongs.Contains(CurrentSong)) CurrentSong = allSongs.FirstOrDefault();
            }
        }

        public Song[] SearchSongs { get; set; }

        public bool IsSearching { get { return !string.IsNullOrEmpty(SearchKey); } }

        public Instance(bool isIndependent)
        {
            Inited = false;
            Player = new MediaElement();
            Player.MediaEnded += Player_MediaEnded;

            if (isIndependent) window = new PlayerWindow(Player);

            isAllShuffle = true;
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            SetNextSong();
        }

        public void Refresh()
        {
            allSongsShuffled = GetShuffledSongs(LoadAllSongs()).ToArray();

            for (int i = 0; i < allSongsShuffled.Length; i++) allSongsShuffled[i].Index = i;

            AllSongs = GetAllSongs(allSongsShuffled, IsAllShuffle).ToArray();
            SearchSongs = GetSearchSongs(allSongsShuffled, IsSearchShuffle).ToArray();

            Inited = true;
        }

        private IEnumerable<Song> LoadAllSongs()
        {
            try
            {
                IEnumerable<string> sourcePaths = GetSourcePaths();
                IEnumerable<string> nonHiddenFiles = sourcePaths.SelectMany(LoadFilePaths).Where(IsNotHidden);

                return nonHiddenFiles.Select(p => new Song(p));
            }
            catch
            {
                return Enumerable.Empty<Song>();
            }
        }

        private IEnumerable<string> GetSourcePaths()
        {
            if (mediaSources != null && mediaSources.Any()) return mediaSources;

            string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            return Enumerable.Repeat(defaultPath, 1);
        }

        private IEnumerable<string> LoadFilePaths(string path)
        {
            if (File.Exists(path)) yield return path;

            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path)) yield return file;
            }
        }

        private bool IsNotHidden(string path)
        {
            FileInfo file = new FileInfo(path);

            return (file.Attributes & FileAttributes.Hidden) == 0;
        }

        private IEnumerable<Song> GetShuffledSongs(IEnumerable<Song> songs)
        {
            return songs.OrderBy(s => ran.Next());
        }

        private IEnumerable<Song> GetAllSongs(IEnumerable<Song> allSongsShuffled, bool isAllShuffle)
        {
            return isAllShuffle ? allSongsShuffled : GetOrderedSongs(allSongsShuffled);
        }

        private IEnumerable<Song> GetSearchSongs(IEnumerable<Song> allSongsShuffled, bool isSearchShuffle)
        {
            if (!isSearchShuffle) return GetFilteredSongs(allSongs);

            return GetFilteredSongs(allSongs).OrderBy(s => Array.IndexOf(AllSongs, s));
        }

        private IEnumerable<Song> GetFilteredSongs(IEnumerable<Song> allSongs)
        {
            if (!IsSearching) return Enumerable.Empty<Song>();

            IEnumerable<Song> mixedTitle = allSongs.Where(CT).OrderBy(TI).ThenBy(s => s.Title).ThenBy(s => s.Artist);
            IEnumerable<Song> mixedArtist = allSongs.Where(CA).OrderBy(AI).ThenBy(s => s.Artist).ThenBy(s => s.Title);
            IEnumerable<Song> lowerTitle = allSongs.Where(CLT).OrderBy(LTI).ThenBy(s => s.Title).ThenBy(s => s.Artist);
            IEnumerable<Song> lowerArtist = allSongs.Where(CLA).OrderBy(LAI).ThenBy(s => s.Artist).ThenBy(s => s.Title);

            return mixedTitle.Concat(mixedArtist).Concat(lowerTitle).Concat(lowerArtist).Distinct();
        }

        #region Filtermethods
        private bool CT(Song song)
        {
            return song.Title.Contains(searchKey);
        }

        private bool CA(Song song)
        {
            return song.Artist.Contains(searchKey);
        }

        private bool CLT(Song song)
        {
            return song.Title.ToLower().Contains(lowerSearchKey);
        }

        private bool CLA(Song song)
        {
            return song.Artist.ToLower().Contains(lowerSearchKey);
        }

        private int TI(Song song)
        {
            return song.Title.IndexOf(searchKey);
        }

        private int AI(Song song)
        {
            return song.Artist.IndexOf(searchKey);
        }

        private int LTI(Song song)
        {
            return song.Title.ToLower().IndexOf(lowerSearchKey);
        }

        private int LAI(Song song)
        {
            return song.Artist.ToLower().IndexOf(lowerSearchKey);
        }
        #endregion

        private IEnumerable<Song> GetOrderedSongs(IEnumerable<Song> allSongs)
        {
            return allSongs.OrderBy(s => s.Title).ThenBy(s => s.Artist);
        }

        public void SetNextSong()
        {
            SetNextSong(IsOnlySearch ? SearchSongs : AllSongs);
        }

        private void SetNextSong(Song[] songs)
        {
            int index = Array.IndexOf(songs, currentSong);

            if (index == -1 && !songs.Any()) return;

            index = (index + 1) % songs.Length;
            CurrentSong = songs.ElementAtOrDefault(index);
        }

        public void SetPreviousSong()
        {
            SetPreviousSong(IsOnlySearch ? SearchSongs : AllSongs);
        }

        public void SetPreviousSong(Song[] songs)
        {
            int index = Array.IndexOf(songs, currentSong);

            if (index == -1)
            {
                if (!songs.Any()) return;
                index = 1;
            }

            index = (index + songs.Length - 1) % songs.Length;
            CurrentSong = songs.ElementAtOrDefault(index);
        }

        public void Dispose()
        {
            Player?.Close();
            window?.Close();
        }
    }
}