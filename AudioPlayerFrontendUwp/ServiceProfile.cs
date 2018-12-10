using AudioPlayerBackend;

namespace AudioPlayerFrontend
{
    public class ServiceProfile
    {
        private bool buildStandalone, buildServer, buildClient, ifNon, reload;
        private bool? isAllShuffle, isSearchShuffle, isOnlySearch, play, isStreaming;
        private int serverPort;
        private int? clientPort;
        private string searchKey, serverAddress;
        private float? volume, clientVolume;
        private string[] mediaSources;

        public bool BuildStandalone { get => buildStandalone; set => buildStandalone = value; }

        public bool BuildServer { get => buildServer; set => buildServer = value; }

        public bool BuildClient { get => buildClient; set => buildClient = value; }

        public bool IfNon { get => ifNon; set => ifNon = value; }

        public bool Reload { get => reload; set => reload = value; }

        public bool? IsAllShuffle { get => isAllShuffle; set => isAllShuffle = value; }

        public bool? IsSearchShuffle { get => isSearchShuffle; set => isSearchShuffle = value; }

        public bool? IsOnlySearch { get => isOnlySearch; set => isOnlySearch = value; }

        public bool? Play { get => play; set => play = value; }

        public bool? IsStreaming { get => isStreaming; set => isStreaming = value; }

        public int ServerPort { get => serverPort; set => serverPort = value; }

        public int? ClientPort { get => clientPort; set => clientPort = value; }

        public string SearchKey { get => searchKey; set => searchKey = value; }

        public string ServerAddress { get => serverAddress; set => serverAddress = value; }

        public float? Volume { get => volume; set => volume = value; }

        public float? ClientVolume { get => clientVolume; set => clientVolume = value; }

        public string[] MediaSources { get => mediaSources; set => mediaSources = value; }

        public ServiceProfile()
        {
        }

        public ServiceProfile(ServiceBuilder sb)
        {
            BuildStandalone = sb.BuildStandalone;
            BuildServer = sb.BuildServer;
            BuildClient = sb.BuildClient;
            IfNon = sb.IfNon;
            Reload = sb.Reload;
            IsAllShuffle = sb.IsAllShuffle;
            IsSearchShuffle = sb.IsSearchShuffle;
            IsOnlySearch = sb.IsOnlySearch;
            Play = sb.Play;
            IsStreaming = sb.IsStreaming;
            ServerPort = sb.ServerPort;
            ClientPort = sb.ClientPort;
            SearchKey = sb.SearchKey;
            ServerAddress = sb.ServerAddress;
            Volume = sb.Volume;
            ClientVolume = sb.ClientVolume;
            MediaSources = sb.MediaSources;
        }

        public void FillServiceBuilder(ServiceBuilder builder)
        {
            if (BuildStandalone) builder.WithStandalone();
            else if (BuildServer) builder.WithServer(serverPort);
            else if (BuildClient) builder.WithClient(ServerAddress, ClientPort);

            builder.WithSetMediaIfNon(IfNon)
                .WithReload(Reload).WithIsAllShuffle(IsAllShuffle)
                .WithIsSearchShuffle(IsSearchShuffle)
                .WithIsOnlySearch(IsOnlySearch)
                .WithPlay(Play)
                .WithIsStreaming(IsStreaming)
                .WithServerPort(ServerPort)
                .WithClientPort(ClientPort)
                .WithSearchKey(SearchKey)
                .WithServerAddress(ServerAddress)
                .WithVolume(Volume)
                .WithClientVolume(clientVolume)
                .WithMediaSources(mediaSources);
        }

        public void ToClient()
        {
            if (BuildClient) return;

            BuildServer = BuildStandalone = false;
            BuildClient = true;

            if (ClientPort == null) ClientPort = 1884;
        }
    }
}
