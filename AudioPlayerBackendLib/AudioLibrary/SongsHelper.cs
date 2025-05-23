﻿using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend.AudioLibrary
{
    static class SongsHelper
    {
        private static readonly Random ran = new Random();

        public static IEnumerable<Song> GetShuffledSongs(IEnumerable<IEnumerable<Song>> allSongs)
        {
            return allSongs.SelectMany(songs => songs).Distinct().OrderBy(s => ran.Next());
        }

        public static bool GetIsSearching(string searchKey)
        {
            return !string.IsNullOrEmpty(searchKey);
        }

        public static IEnumerable<Song> GetAllSongs(Playlist playlist)
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

        public static IEnumerable<Song> GetFilteredSongs(IEnumerable<Song> allSongs, string searchKey)
        {
            if (!GetIsSearching(searchKey)) return allSongs;

            string sk = searchKey;
            string lsk = searchKey.ToLower();

            IEnumerable<Song> caseSenTitle = allSongs.Where(s => CT(s, sk)).
                OrderBy(s => TI(s, sk)).ThenBy(s => s.Title).ThenBy(s => s.Artist).ThenBy(s => s.Index);
            IEnumerable<Song> caseSenArtist = allSongs.Where(s => CA(s, sk)).
                OrderBy(s => AI(s, sk)).ThenBy(s => s.Artist).ThenBy(s => s.Title).ThenBy(s => s.Index);
            IEnumerable<Song> lowerTitle = allSongs.Where(s => CLT(s, lsk)).
                OrderBy(s => LTI(s, lsk)).ThenBy(s => s.Title).ThenBy(s => s.Artist).ThenBy(s => s.Index);
            IEnumerable<Song> lowerArtist = allSongs.Where(s => CLA(s, lsk)).
                OrderBy(s => LAI(s, lsk)).ThenBy(s => s.Artist).ThenBy(s => s.Title).ThenBy(s => s.Index);

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

        public static (Song? song, bool overflow) GetNextSong(Playlist playlist, Song? currentSong = null)
        {
            if (!currentSong.HasValue) currentSong = playlist?.GetCurrentSong();
            return GetNextSong(playlist.Songs, playlist.Shuffle, currentSong);
        }

        public static (Song? song, bool underflow) GetPreviousSong(IEnumerable<Song> allSongsShuffled, OrderType shuffle, Song? currentSong)
        {
            if (!currentSong.HasValue) return (null, false);

            (Song previous, bool found, bool underflow) = GetAllSongs(allSongsShuffled, shuffle)
                .PreviousOrDefault(currentSong.Value);

            return (found ? (Song?)previous : null, underflow);
        }
    }
}
