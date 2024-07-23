using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
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

        public TimeSpan Position { get; }

        public TimeSpan Duration { get; }

        public RequestSong? RequestSong { get; }

        public Guid? CurrentSongId { get; }

        public ICollection<Song> Songs { get; }

        public FileMediaSources FileMediaSources { get; }

        public Playlist(Guid id, PlaylistType type, string name, OrderType shuffle, LoopType loop,
            double playbackRate, TimeSpan position, TimeSpan duration, RequestSong? requestSong,
            Guid? currentSongId, ICollection<Song> songs, FileMediaSources fileMediaSources)
        {
            Id = id;
            Type = type;
            Name = name;
            Shuffle = shuffle;
            Loop = loop;
            PlaybackRate = playbackRate;
            Position = position;
            Duration = duration;
            RequestSong = requestSong;
            CurrentSongId = currentSongId;
            Songs = songs;
            FileMediaSources = fileMediaSources;
        }

        public Song? GetCurrentSong()
        {
            if (CurrentSongId == null) return null;

            return Songs.First(s => s.Id == CurrentSongId);
        }
    }
}
