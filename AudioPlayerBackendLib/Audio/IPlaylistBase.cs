using System;

namespace AudioPlayerBackend.Audio
{
    public enum LoopType { Next, Stop, CurrentPlaylist, CurrentSong }

    public interface IPlaylistBase
    {
        event EventHandler<ValueChangedEventArgs<bool>> IsAllShuffleChanged;
        event EventHandler<ValueChangedEventArgs<LoopType>> LoopChanged;
        event EventHandler<ValueChangedEventArgs<TimeSpan>> PositionChanged;
        event EventHandler<ValueChangedEventArgs<TimeSpan>> DurationChanged;
        event EventHandler<ValueChangedEventArgs<Song?>> CurrentSongChanged;
        event EventHandler<ValueChangedEventArgs<Song[]>> SongsChanged;

        Guid ID { get; }

        bool IsAllShuffle { get; set; }

        LoopType Loop { get; set; }

        TimeSpan Position { get; set; }

        TimeSpan Duration { get; set; }

        Song? CurrentSong { get; set; }

        Song[] Songs { get; set; }
    }
}
