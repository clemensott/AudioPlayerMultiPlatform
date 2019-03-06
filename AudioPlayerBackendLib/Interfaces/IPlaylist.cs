using System;
using System.ComponentModel;

namespace AudioPlayerBackend
{
    public enum LoopType { Next, Stop, CurrentPlaylist, CurrentSong }

    public interface IPlaylist : INotifyPropertyChanged
    {
        Guid ID { get; set; }

        bool IsAllShuffle { get; set; }

        bool IsOnlySearch { get; set; }

        bool IsSearchShuffle { get; set; }

        LoopType Loop { get; set; }

        TimeSpan Position { get; set; }

        TimeSpan Duration { get; set; }

        Song? CurrentSong { get; set; }

        Song[] Songs { get; set; }

        string SearchKey { get; set; }
    }
}
