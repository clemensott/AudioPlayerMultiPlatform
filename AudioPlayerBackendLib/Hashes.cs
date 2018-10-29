namespace AudioPlayerBackendLib
{
    public struct Hashes
    {
        public int MediaSourcesHash { get; set; }

        public int CurrentSongHash { get; set; }

        public int AllSongsHash { get; set; }

        public int SearchSongsHash { get; set; }

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
