using System;
using System.Runtime.Serialization;

namespace AudioPlayerBackendLib
{
    [DataContract]
    public struct States
    {
        [DataMember]
        public TimeSpan Position { get; private set; }

        [DataMember]
        public TimeSpan Duration { get; private set; }

        [DataMember]
        public PlayState PlayState { get; private set; }

        [DataMember]
        public bool IsAllShuffle { get; private set; }

        [DataMember]
        public bool IsSearchShuffle { get; private set; }

        [DataMember]
        public bool IsOnlySearch { get; private set; }

        [DataMember]
        public string SearchKey { get; private set; }

        [DataMember]
        public Hashes Hashes { get; private set; }

        public States(TimeSpan position, TimeSpan duration, PlayState playState, bool isAllShuffle, bool isSearchShuffle,
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

        public States(TimeSpan position, TimeSpan duration, PlayState playState, bool isAllShuffle, bool isSearchShuffle,
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