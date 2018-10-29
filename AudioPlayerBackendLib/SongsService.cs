using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackendLib
{
    static class SongsService
    {
        //public SongsService(IList<Song> allSongsShuffled, bool isAllShuffle, bool isSearchShuffle, string searchKey)

        public static bool GetIsSearching(string searchKey)
        {
            return !string.IsNullOrEmpty(searchKey);
        }

        public static IEnumerable<Song> GetAllSongs(IAudio audio)
        {
            return GetAllSongs(audio.AllSongsShuffled, audio.IsAllShuffle);
        }

        public static IEnumerable<Song> GetAllSongs(IEnumerable<Song> allSongsShuffled, bool isAllShuffle)
        {
            return isAllShuffle ? allSongsShuffled : GetOrderedSongs(allSongsShuffled);
        }

        public static IEnumerable<Song> GetSearchSongs(IAudio audio)
        {
            return GetSearchSongs(audio.AllSongsShuffled, audio.IsSearchShuffle, audio.SearchKey);
        }

        public static IEnumerable<Song> GetSearchSongs(IList<Song> allSongsShuffled, bool isSearchShuffle, string searchKey)
        {
            if (!isSearchShuffle) return GetFilteredSongs(allSongsShuffled, searchKey);

            return GetFilteredSongs(allSongsShuffled, searchKey).OrderBy(s => allSongsShuffled.IndexOf(s));
        }

        private static IEnumerable<Song> GetFilteredSongs(IEnumerable<Song> allSongs, string searchKey)
        {
            if (!GetIsSearching(searchKey)) return Enumerable.Empty<Song>();

            string sk = searchKey;
            string lsk = searchKey.ToLower();

            IEnumerable<Song> mixedTitle = allSongs.Where(s => CT(s, sk)).
                OrderBy(s => TI(s, sk)).ThenBy(s => s.Title).ThenBy(s => s.Artist);
            IEnumerable<Song> mixedArtist = allSongs.Where(s => CA(s, sk)).
                OrderBy(s => AI(s, sk)).ThenBy(s => s.Artist).ThenBy(s => s.Title);
            IEnumerable<Song> lowerTitle = allSongs.Where(s => CLT(s, lsk)).
                OrderBy(s => LTI(s, lsk)).ThenBy(s => s.Title).ThenBy(s => s.Artist);
            IEnumerable<Song> lowerArtist = allSongs.Where(s => CLA(s, lsk)).
                OrderBy(s => LAI(s, lsk)).ThenBy(s => s.Artist).ThenBy(s => s.Title);

            return mixedTitle.Concat(mixedArtist).Concat(lowerTitle).Concat(lowerArtist).Distinct();
        }

        #region Filtermethods
        private static bool CT(Song song, string searchKey)
        {
            return song.Title.Contains(searchKey);
        }

        private static bool CA(Song song, string searchKey)
        {
            return song.Artist.Contains(searchKey);
        }

        private static bool CLT(Song song, string lowerSearchKey)
        {
            return song.Title.ToLower().Contains(lowerSearchKey);
        }

        private static bool CLA(Song song, string lowerSearchKey)
        {
            return song.Artist.ToLower().Contains(lowerSearchKey);
        }

        private static int TI(Song song, string searchKey)
        {
            return song.Title.IndexOf(searchKey);
        }

        private static int AI(Song song, string searchKey)
        {
            return song.Artist.IndexOf(searchKey);
        }

        private static int LTI(Song song, string lowerSearchKey)
        {
            return song.Title.ToLower().IndexOf(lowerSearchKey);
        }

        private static int LAI(Song song, string lowerSearchKey)
        {
            return song.Artist.ToLower().IndexOf(lowerSearchKey);
        }
        #endregion

        private static IEnumerable<Song> GetOrderedSongs(IEnumerable<Song> allSongs)
        {
            return allSongs.OrderBy(s => s.Title).ThenBy(s => s.Artist);
        }

        public static Song? GetNextSong(IAudio audio)
        {
            Song[] songs = (audio.IsOnlySearch ? GetSearchSongs(audio) : GetAllSongs(audio)).ToArray();

            return GetNextSong(songs, audio.CurrentSong);
        }

        public static Song? GetNextSong(Song[] songs, Song? currentSong)
        {
            int index = currentSong.HasValue ? Array.IndexOf(songs, currentSong.Value) : -1;

            if (index == -1 && !songs.Any()) return null;

            index = (index + 1) % songs.Length;

            return songs[index];
        }

        public static Song? GetPreviousSong(IAudio audio)
        {
            Song[] songs = (audio.IsOnlySearch ? GetSearchSongs(audio) : GetAllSongs(audio)).ToArray();

            return GetPreviousSong(songs, audio.CurrentSong);
        }

        public static Song? GetPreviousSong(Song[] songs, Song? currentSong)
        {
            int index = currentSong.HasValue ? Array.IndexOf(songs, currentSong.Value) : -1;

            if (index == -1)
            {
                if (!songs.Any()) return null;
                index = 1;
            }

            index = (index + songs.Length - 1) % songs.Length;

            return songs[index];
        }
    }
}
