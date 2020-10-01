using System;
using System.Linq;
using System.Xml.Serialization;
using AudioPlayerBackend.Audio;

namespace AudioPlayerBackend.Data
{
    public class PlaylistData
    {
        public string ID { get; set; }

        public string Name { get; set; }

        public string CurrentSongPath { get; set; }

        public OrderType Shuffle { get; set; }

        public LoopType Loop { get; set; }

        public long PositionTicks { get; set; }

        [XmlIgnore]
        public TimeSpan Position => new TimeSpan(PositionTicks);

        public long DurationTicks { get; set; }

        [XmlIgnore]
        public TimeSpan Duration => new TimeSpan(DurationTicks);

        public Song[] Songs { get; set; }

        public PlaylistData() { }

        public PlaylistData(IPlaylistBase playlist)
        {
            ID = playlist.ID.ToString();
            Name = playlist.Name;
            CurrentSongPath = playlist.CurrentSong?.FullPath ?? string.Empty;
            Shuffle = playlist.Shuffle;
            Loop = playlist.Loop;
            PositionTicks = playlist.Position.Ticks;
            DurationTicks = playlist.Duration.Ticks;
            Songs = playlist.Songs.ToArray();
        }
    }
}
