using AudioPlayerBackend.Build;

namespace AudioPlayerFrontend
{
    public struct ServiceProfile
    {
        public bool BuildStandalone { get; set; }

        public bool BuildServer { get; set; }

        public bool BuildClient { get; set; }

        public CommunicatorType CommunicatorType { get; set; }

        public bool IfNon { get; set; }

        public bool Reload { get; set; }

        public bool? IsAllShuffle { get; set; }

        public bool? IsSearchShuffle { get; set; }

        public bool? IsOnlySearch { get; set; }

        public bool? Play { get; set; }

        public bool? IsStreaming { get; set; }

        public int ServerPort { get; set; }

        public int? ClientPort { get; set; }

        public string SearchKey { get; set; }

        public string ServerAddress { get; set; }

        public float? Volume { get; set; }

        public float? ClientVolume { get; set; }

        public string[] MediaSources { get; set; }

        public ServiceProfile(ServiceBuilder sb)
        {
            BuildStandalone = sb.BuildStandalone;
            BuildServer = sb.BuildServer;
            BuildClient = sb.BuildClient;
            CommunicatorType = sb.CommunicatorType;
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
            MediaSources = sb.MediaSources;
            ClientVolume = null;
        }

        public void FillServiceBuilder(ServiceBuilder builder)
        {
            if (BuildStandalone) builder.WithStandalone();
            else if (BuildServer) builder.WithServer(ServerPort);
            else if (BuildClient) builder.WithClient(ServerAddress, ClientPort);

            builder.WithCommunicatorType(CommunicatorType)
                .WithSetMediaIfNon(IfNon)
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
                .WithMediaSources(MediaSources);
        }

        public void FillServiceBuilderWithMinimum(ServiceBuilder builder)
        {
            if (BuildStandalone) builder.WithStandalone();
            else if (BuildServer) builder.WithServer(ServerPort);
            else if (BuildClient) builder.WithClient(ServerAddress, ClientPort);

            builder.WithCommunicatorType(CommunicatorType);
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
