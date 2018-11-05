using System;
using NAudio.Wave;

namespace AudioPlayerBackendLib
{
    public interface IAudio
    {
        Song[] AllSongsShuffled { get; set; }

        Song? CurrentSong { get; set; }

        TimeSpan Duration { get; set; }

        bool IsAllShuffle { get; set; }

        bool IsOnlySearch { get; set; }

        bool IsSearchShuffle { get; set; }

        string[] MediaSources { get; set; }

        PlaybackState PlayState { get; set; }

        TimeSpan Position { get; set; }

        string SearchKey { get; set; }

        float Volume { get; set; }
    }
}