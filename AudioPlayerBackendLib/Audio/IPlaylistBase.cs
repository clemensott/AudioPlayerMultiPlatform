using System;

namespace AudioPlayerBackend.Audio
{
    public enum LoopType { Next, Stop, CurrentPlaylist, CurrentSong, StopCurrentSong }

    public enum OrderType { ByTitleAndArtist, ByPath, Custom }

    public interface IPlaylistBase
    {
        event EventHandler<ValueChangedEventArgs<string>> NameChanged;
        event EventHandler<ValueChangedEventArgs<OrderType>> ShuffleChanged;
        event EventHandler<ValueChangedEventArgs<LoopType>> LoopChanged;
        event EventHandler<ValueChangedEventArgs<TimeSpan>> PositionChanged;
        event EventHandler<ValueChangedEventArgs<TimeSpan>> DurationChanged;
        event EventHandler<ValueChangedEventArgs<Song?>> CurrentSongChanged;
        event EventHandler<ValueChangedEventArgs<RequestSong?>> WannaSongChanged;
        event EventHandler<ValueChangedEventArgs<Song[]>> SongsChanged;

        Guid ID { get; }

        string Name { get; set; }

        LoopType Loop { get; set; }

        OrderType Shuffle { get; set; }

        TimeSpan Position { get; set; }

        TimeSpan Duration { get; set; }

        Song? CurrentSong { get; set; }

        RequestSong? WannaSong { get; set; }

        Song[] Songs { get; set; }
    }
}
