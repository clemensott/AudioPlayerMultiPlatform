﻿using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public class Playlist
    {
        public Guid Id { get; }

        public PlaylistType Type { get; }

        public string Name { get; }

        public OrderType Shuffle { get; }

        public LoopType Loop { get; }

        public double PlaybackRate { get; }

        public RequestSong? RequestSong { get; }

        public ICollection<Song> Songs { get; }

        public FileMediaSources FileMediaSources { get; }

        public Guid? NextPlaylist { get; }

        public DateTime? FilesLastUpdated { get; }

        public DateTime? SongsLastUpdated { get; }

        public Playlist(Guid id, PlaylistType type, string name, OrderType shuffle, LoopType loop,
            double playbackRate, RequestSong? requestSong, ICollection<Song> songs, FileMediaSources fileMediaSources,
            Guid? nextPlaylist, DateTime? filesLastUpdated, DateTime? songsLastUpdated)
        {
            Id = id;
            Type = type;
            Name = name;
            Shuffle = shuffle;
            Loop = loop;
            PlaybackRate = playbackRate;
            RequestSong = requestSong;
            Songs = songs;
            FileMediaSources = fileMediaSources;
            NextPlaylist = nextPlaylist;
            FilesLastUpdated = filesLastUpdated;
            SongsLastUpdated = songsLastUpdated;
        }

        public Song? GetCurrentSong()
        {
            if (RequestSong == null) return null;

            return Songs.First(s => s.Id == RequestSong?.Song.Id);
        }
    }
}
