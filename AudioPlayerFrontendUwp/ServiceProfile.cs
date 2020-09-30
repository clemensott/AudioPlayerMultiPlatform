using AudioPlayerBackend.Build;

namespace AudioPlayerFrontend
{
    public struct ServiceProfile
    {
        public bool BuildStandalone { get; set; }

        public bool BuildServer { get; set; }

        public bool BuildClient { get; set; }

        public CommunicatorProtocol CommunicatorProtocol { get; set; }

        public bool? IsAllShuffle { get; set; }

        public bool? IsSearchShuffle { get; set; }

        public bool? Play { get; set; }

        public bool? IsStreaming { get; set; }

        public int ServerPort { get; set; }

        public int? ClientPort { get; set; }

        public string SearchKey { get; set; }

        public string ServerAddress { get; set; }

        public float? Volume { get; set; }

        public float? ClientVolume { get; set; }

        public ServiceProfile(ServiceBuilder sb)
        {
            BuildStandalone = sb.BuildStandalone;
            BuildServer = sb.BuildServer;
            BuildClient = sb.BuildClient;
            CommunicatorProtocol = sb.CommunicatorProtocol;
            IsAllShuffle = sb.IsAllShuffle;
            IsSearchShuffle = sb.IsSearchShuffle;
            Play = sb.Play;
            IsStreaming = sb.IsStreaming;
            ServerPort = sb.ServerPort;
            ClientPort = sb.ClientPort;
            SearchKey = sb.SearchKey;
            ServerAddress = sb.ServerAddress;
            Volume = sb.Volume;
            ClientVolume = null;
        }

        public void FillServiceBuilder(ServiceBuilder builder)
        {
            if (BuildStandalone) builder.WithStandalone();
            else if (BuildServer) builder.WithServer(ServerPort);
            else if (BuildClient) builder.WithClient(ServerAddress, ClientPort);

            builder
                .WithCommunicatorProtocol(CommunicatorProtocol)
                .WithIsAllShuffle(IsAllShuffle)
                .WithIsSearchShuffle(IsSearchShuffle)
                .WithPlay(Play)
                .WithIsStreaming(IsStreaming)
                .WithServerPort(ServerPort)
                .WithClientPort(ClientPort)
                .WithSearchKey(SearchKey)
                .WithServerAddress(ServerAddress)
                .WithVolume(Volume);
        }
    }
}
