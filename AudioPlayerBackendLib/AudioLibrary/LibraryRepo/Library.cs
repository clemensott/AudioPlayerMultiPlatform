﻿using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo
{
    public class Library
    {
        public PlaybackState PlayState { get; }

        public double Volume { get; }

        public Guid? CurrentPlaylistId { get; }

        public ICollection<PlaylistInfo> Playlists { get; }

        public DateTime? FoldersLastUpdated { get; }

        public Library(PlaybackState playState, double volume, Guid? currentPlaylistId, 
            ICollection<PlaylistInfo> playlists, DateTime? foldersLastUpdated)
        {
            PlayState = playState;
            Volume = volume;
            CurrentPlaylistId = currentPlaylistId;
            Playlists = playlists;
            FoldersLastUpdated = foldersLastUpdated;
        }

        public PlaylistInfo GetCurrentPlaylist()
        {
            if (!CurrentPlaylistId.HasValue) return null;

            return Playlists.First(p => p.Id == CurrentPlaylistId);
        }
    }
}
