using System;
using System.Linq;
using System.Xml.Serialization;
using AudioPlayerBackend.Audio;

namespace AudioPlayerBackend.Data
{
    public class PlaylistData
    {
        public string CurrentSongPath { get; set; }

        public bool IsAllShuffle { get; set; }

        public LoopType Loop { get; set; }

        public long PositionTicks { get; set; }

        [XmlIgnore]
        public TimeSpan Position => new TimeSpan(PositionTicks);

        public long DurationTicks { get; set; }

        [XmlIgnore]
        public TimeSpan Duration => new TimeSpan(DurationTicks);

        public string[] Songs { get; set; }

        public PlaylistData() { }

        public PlaylistData(IPlaylistBase playlist)
        {
            CurrentSongPath = playlist.CurrentSong?.FullPath ?? string.Empty;
            IsAllShuffle = playlist.IsAllShuffle;
            Loop = playlist.Loop;
            PositionTicks = playlist.Position.Ticks;
            DurationTicks = playlist.Duration.Ticks;
            Songs = playlist.Songs.Select(s => s.FullPath).ToArray();
        }
    }
}
