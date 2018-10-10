using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerBackendLib
{
    [DataContract]
    public struct Hashes
    {
        [DataMember]
        public int MediaSourcesHash { get; private set; }

        [DataMember]
        public int CurrentSongHash { get; private set; }

        [DataMember]
        public int AllSongsHash { get; private set; }

        [DataMember]
        public int SearchSongsHash { get; private set; }

        public Hashes(object mediaSources, object currentSong, object allSongs, object searchSongs) : this()
        {
            MediaSourcesHash = mediaSources.GetHashCode();
            CurrentSongHash = currentSong.GetHashCode();
            AllSongsHash = allSongs.GetHashCode();
            SearchSongsHash = searchSongs.GetHashCode();
        }

        public Hashes(int mediaSourcesHash, int currentSongHash, int allSongsHash, int searchSongsHash) : this()
        {
            MediaSourcesHash = mediaSourcesHash;
            CurrentSongHash = currentSongHash;
            AllSongsHash = allSongsHash;
            SearchSongsHash = searchSongsHash;
        }
    }
}
