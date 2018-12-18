using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AudioPlayerBackend
{
    public enum LoopType { Next, Stop, This, CurrentSong }

    public interface IPlaylist : INotifyPropertyChanged
    {
        Guid ID { get; }

        bool IsAllShuffle { get; set; }

        bool IsOnlySearch { get; set; }

        bool IsSearchShuffle { get; set; }

        LoopType Loop { get; set; }

        TimeSpan Position { get; set; }

        TimeSpan Duration { get; set; }

        Song? CurrentSong { get; set; }

        IEnumerable<Song> Songs { get; }
    }
}
