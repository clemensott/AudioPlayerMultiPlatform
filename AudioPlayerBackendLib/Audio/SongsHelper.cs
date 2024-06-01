using AudioPlayerBackend.AudioLibrary;
using StdOttStandard.Linq;
using StdOttStandard.Linq.Sort;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend.Audio
{
    static class SongsHelper
    {
        private static readonly Random ran = new Random();

        public static IEnumerable<Song> GetShuffledSongs(IEnumerable<IPlaylistBase> playlists)
        {
            return playlists.SelectMany(p => p.Songs).Distinct().OrderBy(s => ran.Next());
        }

        public static bool GetIsSearching(string searchKey)
        {
            return !string.IsNullOrEmpty(searchKey);
        }

        public static IEnumerable<Song> GetAllSongs(IPlaylistBase playlist)
        {
            return GetAllSongs(playlist.Songs, playlist.Shuffle);
        }

        public static IEnumerable<Song> GetAllSongs(IEnumerable<Song> allSongsShuffled, OrderType shuffle)
        {
            switch (shuffle)
            {
                case OrderType.ByTitleAndArtist:
                    return allSongsShuffled.OrderBy(s => s.Title).ThenBy(s => s.Artist);

                case OrderType.ByPath:
                    return allSongsShuffled.OrderBy(s => s.FullPath);
                    
                case OrderType.Custom:
                    return allSongsShuffled;
            }

            throw new ArgumentException("Type is not implemented: " + shuffle, nameof(shuffle));
        }

        public static IEnumerable<Song> GetSearchSongs(IAudioService service)
        {
            return GetSearchSongs(service.AllSongs, service.IsSearchShuffle, service.SearchKey);
        }

        public static IEnumerable<Song> GetSearchSongs(IEnumerable<Song> allSongsShuffled, bool isSearchShuffle, string searchKey)
        {
            if (!isSearchShuffle) return GetFilteredSongs(allSongsShuffled, searchKey);

            return GetFilteredSongs(allSongsShuffled, searchKey).HeapSort(allSongsShuffled.IndexOf);
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
            return song.Title?.Contains(searchKey) == true;
        }

        private static bool CA(Song song, string searchKey)
        {
            return song.Artist?.Contains(searchKey) == true;
        }

        private static bool CLT(Song song, string lowerSearchKey)
        {
            return song.Title?.ToLower().Contains(lowerSearchKey) == true;
        }

        private static bool CLA(Song song, string lowerSearchKey)
        {
            return song.Artist?.ToLower().Contains(lowerSearchKey) == true;
        }

        private static int TI(Song song, string searchKey)
        {
            return song.Title?.IndexOf(searchKey) ?? -1;
        }

        private static int AI(Song song, string searchKey)
        {
            return song.Artist?.IndexOf(searchKey) ?? -1;
        }

        private static int LTI(Song song, string lowerSearchKey)
        {
            return song.Title?.ToLower().IndexOf(lowerSearchKey) ?? -1;
        }

        private static int LAI(Song song, string lowerSearchKey)
        {
            return song.Artist?.ToLower().IndexOf(lowerSearchKey) ?? -1;
        }
        #endregion

        public static (Song? song, bool overflow) GetNextSong(IEnumerable<Song> allSongsShuffled, OrderType shuffle, Song? currentSong = null)
        {
            if (!currentSong.HasValue) return (null, false);

            (Song next, bool found, bool overflow) = GetAllSongs(allSongsShuffled, shuffle)
                .NextOrDefault(currentSong.Value);

            return (found ? (Song?)next : null, overflow);
        }

        public static (Song? song, bool overflow) GetNextSong(IPlaylistBase playlist, Song? currentSong = null)
        {
            if (!currentSong.HasValue) currentSong = playlist?.CurrentSong;
            return GetNextSong(playlist.Songs, playlist.Shuffle, currentSong);
        }

        public static (Song? song, bool underflow) GetPreviousSong(IPlaylistBase playlist)
        {
            if (!(playlist?.CurrentSong).HasValue) return (null, false);

            (Song next, bool found, bool underflow) = GetAllSongs(playlist)
                .PreviousOrDefault(playlist.CurrentSong.Value);

            return (found ? (Song?)next : null, underflow);
        }
    }
}
