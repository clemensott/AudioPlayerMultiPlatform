using NAudio.Wave;
using System;

namespace AudioPlayerBackendLib
{
    public struct States
    {
        public TimeSpan Position { get; set; }

        public TimeSpan Duration { get; set; }

        public PlaybackState PlayState { get; set; }

        public bool IsAllShuffle { get; set; }

        public bool IsSearchShuffle { get; set; }

        public bool IsOnlySearch { get; set; }

        public string SearchKey { get; set; }

        public Hashes Hashes { get; set; }

        public States(TimeSpan position, TimeSpan duration, PlaybackState playState, bool isAllShuffle, bool isSearchShuffle,
            bool isOnlySearch, object mediaSources, object currentSong, object allSongs, object searchSongs) : this()
        {
            Position = position;
            Duration = duration;
            PlayState = playState;
            IsAllShuffle = isAllShuffle;
            IsSearchShuffle = isSearchShuffle;
            IsOnlySearch = isOnlySearch;
            Hashes = new Hashes(mediaSources.GetHashCode(), currentSong.GetHashCode(), allSongs.GetHashCode(), searchSongs.GetHashCode());
        }

        public States(TimeSpan position, TimeSpan duration, PlaybackState playState, bool isAllShuffle, bool isSearchShuffle,
            bool isOnlySearch, string searchKey, Hashes hashes) : this()
        {
            Position = position;
            Duration = duration;
            PlayState = playState;
            IsAllShuffle = isAllShuffle;
            IsSearchShuffle = isSearchShuffle;
            IsOnlySearch = isOnlySearch;
            SearchKey = searchKey;
            Hashes = hashes;
        }
    }
}