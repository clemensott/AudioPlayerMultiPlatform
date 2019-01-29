using StdOttStandard;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend
{
    static class SongsService
    {
        public static bool GetIsSearching(string searchKey)
        {
            return !string.IsNullOrEmpty(searchKey);
        }

        public static IEnumerable<Song> GetAllSongs(IPlaylist playlist)
        {
            return GetAllSongs(playlist.Songs, playlist.IsAllShuffle);
        }

        public static IEnumerable<Song> GetAllSongs(IEnumerable<Song> allSongsShuffled, bool isAllShuffle)
        {
            return isAllShuffle ? allSongsShuffled : GetOrderedSongs(allSongsShuffled);
        }

        public static IEnumerable<Song> GetSearchSongs(IPlaylist playlist)
        {
            return GetSearchSongs(playlist.Songs, playlist.IsSearchShuffle, playlist.SearchKey);
        }

        public static IEnumerable<Song> GetSearchSongs(IEnumerable<Song> allSongsShuffled, bool isSearchShuffle, string searchKey)
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

        public static (Song? song, bool overflow) GetNextSong(IPlaylist playlist)
        {
            return (playlist.IsOnlySearch && GetIsSearching(playlist.SearchKey) ? GetSearchSongs(playlist) : GetAllSongs(playlist))
                   .Cast<Song?>().NextOrDefault(playlist.CurrentSong);
        }

        public static (Song? song, bool underflow) GetPreviousSong(IPlaylist playlist)
        {
            return (playlist.IsOnlySearch && GetIsSearching(playlist.SearchKey) ? GetSearchSongs(playlist) : GetAllSongs(playlist))
                   .Cast<Song?>().PreviousOrDefault(playlist.CurrentSong);
        }
    }
}
