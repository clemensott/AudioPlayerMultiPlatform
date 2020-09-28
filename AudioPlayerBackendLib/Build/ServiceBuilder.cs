using System;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.MQTT;
using AudioPlayerBackend.Player;
using StdOttStandard.CommandlineParser;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AudioPlayerBackend.Communication.OwnTcp;
using AudioPlayerBackend.Data;
using StdOttStandard.Linq;

namespace AudioPlayerBackend.Build
{
    public class ServiceBuilder : INotifyPropertyChanged
    {
        private readonly IServiceBuilderHelper helper;
        private bool? isAllShuffle, isSearchShuffle, play, isStreaming;
        private int serverPort;
        private int? clientPort;
        private string searchKey, serverAddress, dataFilePath;
        private float? volume;
        private CommunicatorProtocol communicatorProtocol;
        private IPlayer player;
        private INotifyPropertyChangedHelper notifyPropertyChangedHelper;

        public bool BuildStandalone { get; private set; }

        public bool BuildServer { get; private set; }

        public bool BuildClient { get; private set; }

        public CommunicatorProtocol CommunicatorProtocol
        {
            get => communicatorProtocol;
            set
            {
                if (value == communicatorProtocol) return;

                communicatorProtocol = value;
                OnPropertyChanged(nameof(CommunicatorProtocol));
            }
        }

        public bool? IsAllShuffle
        {
            get => isAllShuffle;
            set
            {
                if (value == isAllShuffle) return;

                isAllShuffle = value;
                OnPropertyChanged(nameof(IsAllShuffle));
            }
        }

        public bool? IsSearchShuffle
        {
            get => isSearchShuffle;
            set
            {
                if (value == isSearchShuffle) return;

                isSearchShuffle = value;
                OnPropertyChanged(nameof(IsSearchShuffle));
            }
        }

        public string SearchKey
        {
            get => searchKey;
            set
            {
                if (value == searchKey) return;

                searchKey = value;
                OnPropertyChanged(nameof(SearchKey));
            }
        }

        public bool? Play
        {
            get => play;
            set
            {
                if (value == play) return;

                play = value;
                OnPropertyChanged(nameof(Play));
            }
        }

        public bool? IsStreaming
        {
            get => isStreaming;
            set
            {
                if (value == isStreaming) return;

                isStreaming = value;
                OnPropertyChanged(nameof(IsStreaming));
            }
        }

        public int ServerPort
        {
            get => serverPort;
            set
            {
                if (value == serverPort) return;

                serverPort = value;
                OnPropertyChanged(nameof(ServerPort));
            }
        }

        public int? ClientPort
        {
            get => clientPort;
            set
            {
                if (value == clientPort) return;

                clientPort = value;
                OnPropertyChanged(nameof(ClientPort));
            }
        }

        public string ServerAddress
        {
            get => serverAddress;
            set
            {
                if (value == serverAddress) return;

                serverAddress = value;
                OnPropertyChanged(nameof(ServerAddress));
            }
        }

        public string DataFilePath
        {
            get => dataFilePath;
            set
            {
                if (value == dataFilePath) return;

                dataFilePath = value;
                OnPropertyChanged(nameof(DataFilePath));
            }
        }

        public float? Volume
        {
            get => volume;
            set
            {
                if (value == volume) return;

                volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        public IPlayer Player
        {
            get => player;
            set
            {
                if (value == player) return;

                player = value;
                OnPropertyChanged(nameof(Player));
            }
        }

        public INotifyPropertyChangedHelper NotifyPropertyChangedHelper
        {
            get => notifyPropertyChangedHelper;
            set
            {
                if (value == notifyPropertyChangedHelper) return;

                notifyPropertyChangedHelper = value;
                OnPropertyChanged(nameof(NotifyPropertyChangedHelper));
            }
        }

        public ServiceBuilder(IServiceBuilderHelper helper = null)
        {
            this.helper = helper;

            WithStandalone();
        }

        public ServiceBuilder WithArgs(IEnumerable<string> args)
        {
            OptionParsed parsed;
            Option clientOpt = new Option("c", "client", "Starts the app as client with the following server address and port", false, 3, 2);
            Option serverOpt = new Option("s", "server", "Starts the app as server with the following port", false, 2, 2);
            Option sourcesOpt = new Option("m", "media-sources", "Files and directories to play", false, -1, 0);
            Option allShuffleOpt = Option.GetLongOnly("all-shuffle", "Shuffles all songs.", false, 0, 0);
            Option searchShuffleOpt = Option.GetLongOnly("search-shuffle", "Shuffles all songs.", false, 0, 0);
            Option searchKeyOpt = Option.GetLongOnly("search-key", "Shuffles all songs.", false, 1, 0);
            Option playOpt = new Option("p", "play", "Starts playback on startup", false, 0, 0);
            Option serviceVolOpt = new Option("v", "volume", "The volume of service (value between 0 and 1)", false, 1, 1);
            Option streamingOpt = Option.GetLongOnly("stream", "If given the audio is streamed to the client", false, 0, 0);
            Option dataFileOpt = new Option("d", "data-file", "Filepath to where to read and write data.", false, 1, 1);

            Options options = new Options(sourcesOpt, clientOpt, serverOpt, playOpt,
                allShuffleOpt, searchShuffleOpt, searchKeyOpt, serviceVolOpt, streamingOpt, dataFileOpt);
            OptionParseResult result = options.Parse(args);

            if (result.TryGetFirstValidOptionParseds(serverOpt, out parsed))
            {
                WithCommunicatorProtocol((CommunicatorProtocol)Enum
                        .Parse(typeof(CommunicatorProtocol), parsed.Values[0], true))
                    .WithServer(int.Parse(parsed.Values[1]));
            }

            if (result.TryGetFirstValidOptionParseds(clientOpt, out parsed))
            {
                WithCommunicatorProtocol((CommunicatorProtocol)Enum
                    .Parse(typeof(CommunicatorProtocol), parsed.Values[0], true));

                if (parsed.Values.Count > 2) WithClient(parsed.Values[1], int.Parse(parsed.Values[2]));
                else WithClient(parsed.Values[1]);
            }

            if (result.TryGetFirstValidOptionParseds(allShuffleOpt, out parsed)) WithIsAllShuffle();
            if (result.TryGetFirstValidOptionParseds(searchShuffleOpt, out parsed)) WithIsSearchShuffle();
            if (result.TryGetFirstValidOptionParseds(searchKeyOpt, out parsed)) WithSearchKey(parsed.Values.FirstOrDefault());
            if (result.HasValidOptionParseds(playOpt)) WithPlay();

            if (result.TryGetFirstValidOptionParseds(serviceVolOpt, out parsed)) WithVolume(float.Parse(parsed.Values[0]));

            if (result.HasValidOptionParseds(streamingOpt)) WithIsStreaming();

            if (result.TryGetFirstValidOptionParseds(dataFileOpt, out parsed)) DataFilePath = parsed.Values[0];

            return this;
        }

        public ServiceBuilder WithService(IAudioService service)
        {
            return WithIsAllShuffle(GetSharedValueOrNull(service.GetAllPlaylists().Select(p => p.IsAllShuffle)))
                .WithIsSearchShuffle(service.IsSearchShuffle)
                .WithSearchKey(service.SearchKey)
                //.WithPlay(service.PlayState == PlaybackState.Playing)
                .WithVolume(service.Volume);
        }

        private static bool? GetSharedValueOrNull(IEnumerable<bool> values)
        {
            bool[] distinctValues = values.ToNotNull().Distinct().ToArray();
            return distinctValues.Length == 1 ? (bool?)distinctValues[0] : null;
        }

        public ServiceBuilder WithStandalone()
        {
            BuildStandalone = true;
            BuildServer = false;
            BuildClient = false;

            return this;
        }

        public ServiceBuilder WithServer(int port)
        {
            BuildStandalone = false;
            BuildServer = true;
            BuildClient = false;

            return WithServerPort(port);
        }

        public ServiceBuilder WithCommunicatorProtocol(CommunicatorProtocol communicatorProtocol)
        {
            CommunicatorProtocol = communicatorProtocol;
            return this;
        }

        public ServiceBuilder WithMqtt()
        {
            CommunicatorProtocol = CommunicatorProtocol.MQTT;
            return this;
        }

        public ServiceBuilder WithOwnTcp()
        {
            CommunicatorProtocol = CommunicatorProtocol.OwnTCP;
            return this;
        }

        public ServiceBuilder WithServerPort(int port)
        {
            ServerPort = port;

            return this;
        }

        public ServiceBuilder WithClient(string serverAddress, int? port = null)
        {
            BuildStandalone = false;
            BuildServer = false;
            BuildClient = true;

            return WithServerAddress(serverAddress).WithClientPort(port);
        }

        public ServiceBuilder WithCommunicator(ICommunicator communicator)
        {
            if (communicator is IClientCommunicator)
            {
                IClientCommunicator clientCommunicator = (IClientCommunicator)communicator;
                return WithClient(clientCommunicator.ServerAddress, clientCommunicator.Port);
            }
            else if (communicator is IServerCommunicator)
            {
                IServerCommunicator serverCommunicator = (IServerCommunicator)communicator;
                return WithServer(serverCommunicator.Port);
            }

            return WithStandalone();
        }

        public ServiceBuilder WithServerAddress(string serverAddress)
        {
            ServerAddress = serverAddress;

            return this;
        }

        public ServiceBuilder WithClientPort(int? port)
        {
            ClientPort = port;

            return this;
        }

        public ServiceBuilder WithIsAllShuffle(bool? value = true)
        {
            IsAllShuffle = value;

            return this;
        }

        public ServiceBuilder WithIsSearchShuffle(bool? value = true)
        {
            IsSearchShuffle = value;

            return this;
        }

        public ServiceBuilder WithSearchKey(string value)
        {
            SearchKey = value;

            return this;
        }

        public ServiceBuilder WithPlay(bool? value = true)
        {
            Play = value;

            return this;
        }

        public ServiceBuilder WithVolume(float? volume)
        {
            Volume = volume;

            return this;
        }

        public ServiceBuilder WithIsStreaming(bool? value = true)
        {
            IsStreaming = value;

            return this;
        }

        public ServiceBuilder WithPlayer(IPlayer player)
        {
            Player = player;

            return this;
        }

        public ServiceBuilder WithNotifyPropertyChangedHelper(INotifyPropertyChangedHelper helper)
        {
            NotifyPropertyChangedHelper = helper;

            return this;
        }

        public ICommunicator CreateCommunicator()
        {
            switch (CommunicatorProtocol)
            {
                case CommunicatorProtocol.MQTT:
                    if (BuildServer) return CreateMqttServerCommunicator(ServerPort);
                    if (BuildClient) return CreateMqttClientCommunicator(ServerAddress, ClientPort);
                    break;

                case CommunicatorProtocol.OwnTCP:
                    if (BuildServer) return CreateOwnTcpServerCommunicator(ServerPort);
                    if (BuildClient) return CreateOwnTcpClientCommunicator(ServerAddress, ClientPort ?? 1884);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            return null;
        }

        public IServicePlayer CreateServicePlayer(IAudioService service)
        {
            if (BuildClient) return CreateAudioStreamPlayer(Player, service);
            return CreateAudioServicePlayer(Player, service);
        }

        public ReadWriteAudioServiceData CompleteService(IAudioService service)
        {
            if (IsAllShuffle.HasValue)
            {
                foreach (IPlaylist playlist in service.SourcePlaylists)
                {
                    playlist.IsAllShuffle = IsAllShuffle.Value;
                }

                foreach (IPlaylist playlist in service.Playlists)
                {
                    playlist.IsAllShuffle = IsAllShuffle.Value;
                }
            }

            if (IsSearchShuffle.HasValue) service.IsSearchShuffle = IsSearchShuffle.Value;
            if (SearchKey != null) service.SearchKey = SearchKey;
            if (play.HasValue) service.PlayState = play.Value ? PlaybackState.Playing : PlaybackState.Paused;
            if (volume.HasValue) service.Volume = volume.Value;

            return ReadWriteAudioServiceData.Start(DataFilePath, service, NotifyPropertyChangedHelper);
        }

        protected virtual MqttClientCommunicator CreateMqttClientCommunicator(string serverAddress, int? port)
        {
            return new MqttClientCommunicator(serverAddress, port, NotifyPropertyChangedHelper);
        }

        protected virtual MqttServerCommunicator CreateMqttServerCommunicator(int port)
        {
            return new MqttServerCommunicator(port, NotifyPropertyChangedHelper);
        }

        protected virtual OwnTcpClientCommunicator CreateOwnTcpClientCommunicator(string serverAddress, int port)
        {
            return new OwnTcpClientCommunicator(serverAddress, port, NotifyPropertyChangedHelper);
        }

        protected virtual OwnTcpServerCommunicator CreateOwnTcpServerCommunicator(int port)
        {
            return new OwnTcpServerCommunicator(port, NotifyPropertyChangedHelper);
        }

        protected virtual AudioStreamPlayer CreateAudioStreamPlayer(IPlayer player, IAudioService service)
        {
            return helper.CreateAudioStreamPlayer(player, service);
        }

        protected virtual AudioServicePlayer CreateAudioServicePlayer(IPlayer player, IAudioService service)
        {
            return helper.CreateAudioServicePlayer(player, service);
        }

        public ServiceBuilder Clone()
        {
            return new ServiceBuilder(helper)
            {
                BuildClient = BuildClient,
                BuildServer = BuildServer,
                BuildStandalone = BuildStandalone,
                ClientPort = ClientPort,
                CommunicatorProtocol = CommunicatorProtocol,
                DataFilePath = DataFilePath,
                IsAllShuffle = IsAllShuffle,
                IsSearchShuffle = IsSearchShuffle,
                IsStreaming = IsStreaming,
                NotifyPropertyChangedHelper = NotifyPropertyChangedHelper,
                Play = Play,
                Player = Player,
                SearchKey = SearchKey,
                ServerAddress = ServerAddress,
                ServerPort = ServerPort,
                Volume = Volume,
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged == null) return;

            if (helper?.InvokeDispatcher != null) helper.InvokeDispatcher(Raise);
            else Raise();

            void Raise() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
