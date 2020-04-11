using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend.Audio
{
    static class SongsService
    {
        private static readonly Random ran = new Random();

        public static IEnumerable<Song> GetShuffledSongs(IEnumerable<Song> songs)
        {
            return songs.OrderBy(s => ran.Next());
        }

        public static bool GetIsSearching(string searchKey)
        {
            return !string.IsNullOrEmpty(searchKey);
        }

        public static IEnumerable<Song> GetAllSongs(IPlaylistBase playlist)
        {
            return GetAllSongs(playlist.Songs, playlist.IsAllShuffle);
        }

        public static IEnumerable<Song> GetAllSongs(IEnumerable<Song> allSongsShuffled, bool isAllShuffle)
        {
            return isAllShuffle ? allSongsShuffled : GetOrderedSongs(allSongsShuffled);
        }

        public static IEnumerable<Song> GetSearchSongs(ISourcePlaylist playlist)
        {
            return GetSearchSongs(playlist.ShuffledSongs, playlist.IsSearchShuffle, playlist.SearchKey);
        }

        public static IEnumerable<Song> GetSearchSongs(IEnumerable<Song> allSongsShuffled, bool isSearchShuffle, string searchKey)
        {
            if (!isSearchShuffle) return GetFilteredSongs(allSongsShuffled, searchKey);

            return GetFilteredSongs(allSongsShuffled, searchKey).OrderBy(allSongsShuffled.IndexOf);
        }

        private static IEnumerable<Song> GetFilteredSongs(IEnumerable<Song> allSongs, string searchKey)
        {
            if (!GetIsSearching(searchKey)) return allSongs;

            string sk = searchKey;
            string lsk = searchKey.ToLower();

            IEnumerable<Song> caseSenTitle = allSongs.Where(s => CT(s, sk)).
                OrderBy(s => TI(s, sk)).ThenBy(s => s.Title).ThenBy(s => s.Artist);
            IEnumerable<Song> caseSenArtist = allSongs.Where(s => CA(s, sk)).
                OrderBy(s => AI(s, sk)).ThenBy(s => s.Artist).ThenBy(s => s.Title);
            IEnumerable<Song> lowerTitle = allSongs.Where(s => CLT(s, lsk)).
                OrderBy(s => LTI(s, lsk)).ThenBy(s => s.Title).ThenBy(s => s.Artist);
            IEnumerable<Song> lowerArtist = allSongs.Where(s => CLA(s, lsk)).
                OrderBy(s => LAI(s, lsk)).ThenBy(s => s.Artist).ThenBy(s => s.Title);

            return caseSenTitle.Concat(caseSenArtist).Concat(lowerTitle).Concat(lowerArtist).Distinct();
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

        public static (Song? song, bool overflow) GetNextSong(IPlaylistBase playlist)
        {
            if (!playlist.CurrentSong.HasValue) return (null, false);

            (Song next, bool found, bool overflow) = GetAllSongs(playlist)
                .NextOrDefault(playlist.CurrentSong.Value);

            return (found ? (Song?)next : null, overflow);
        }

        public static (Song? song, bool underflow) GetPreviousSong(IPlaylistBase playlist)
        {
            if (!playlist.CurrentSong.HasValue) return (null, false);

            (Song next, bool found, bool underflow) = GetAllSongs(playlist)
                .PreviousOrDefault(playlist.CurrentSong.Value);

            return (found ? (Song?)next : null, underflow);
        }
    }
}
