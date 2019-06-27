using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.MQTT;
using AudioPlayerBackend.Player;
using StdOttStandard;
using StdOttStandard.CommendlinePaser;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AudioPlayerBackend
{
    public class ServiceBuilder : INotifyPropertyChanged
    {
        private readonly IServiceBuilderHelper helper;
        private bool ifNon, reload;
        private bool? isAllShuffle, isSearchShuffle, isOnlySearch, play, isStreaming;
        private int serverPort;
        private int? clientPort;
        private string searchKey, serverAddress;
        private float? volume;
        private string[] mediaSources;
        private IAudioService service;
        private ICommunicator communicator;
        private IServicePlayer servicePlayer;
        private IWaveProviderPlayer player;

        public bool BuildStandalone { get; private set; }

        public bool BuildServer { get; private set; }

        public bool BuildClient { get; private set; }

        public bool IfNon
        {
            get => ifNon;
            set
            {
                if (value == ifNon) return;

                ifNon = value;
                OnPropertyChanged(nameof(IfNon));
            }
        }

        public bool Reload
        {
            get => reload;
            set
            {
                if (value == reload) return;

                reload = value;
                OnPropertyChanged(nameof(Reload));
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

        public bool? IsOnlySearch
        {
            get => isOnlySearch;
            set
            {
                if (value == isOnlySearch) return;

                isOnlySearch = value;
                OnPropertyChanged(nameof(IsOnlySearch));
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

        public string[] MediaSources
        {
            get => mediaSources;
            set
            {
                if (value.BothNullOrSequenceEqual(mediaSources)) return;

                mediaSources = value;
                OnPropertyChanged(nameof(MediaSources));
            }
        }

        public IAudioService Service
        {
            get => service;
            set
            {
                if (value == service) return;

                service = value;
                OnPropertyChanged(nameof(Service));
            }
        }

        public IWaveProviderPlayer Player
        {
            get => player;
            set
            {
                if (value == player) return;

                player = value;
                OnPropertyChanged(nameof(Player));
            }
        }

        public ServiceBuilder(IServiceBuilderHelper helper = null)
        {
            this.helper = helper;

            WithStandalone();
        }

        public ServiceBuilder WithArgs(IEnumerable<string> args)
        {
            Option clientOpt = new Option("c", "client", "Starts the app as client with the following server address and port", false, 2, 1);
            Option serverOpt = new Option("s", "server", "Starts the app as server with the following port", false, 1, 1);
            Option sourcesOpt = new Option("m", "media-sources", "Files and directories to play", false, -1, 0);
            Option ifNonOpt = new Option("i", "if-non", "If given the Media sources are only used if there are non", false, 0, 0);
            Option reloadOpt = new Option("r", "reload", "Forces to reload", false, 0, 0);
            Option allShuffleOpt = Option.GetLongOnly("all-shuffle", "Shuffles all songs.", false, 0, 0);
            Option searchShuffleOpt = Option.GetLongOnly("search-shuffle", "Shuffles all songs.", false, 0, 0);
            Option onlySearchOpt = Option.GetLongOnly("only-search", "Shuffles all songs.", false, 0, 0);
            Option searchKeyOpt = Option.GetLongOnly("search-key", "Shuffles all songs.", false, 1, 0);
            Option playOpt = new Option("p", "play", "Starts playback on startup", false, 0, 0);
            Option serviceVolOpt = new Option("v", "volume", "The volume of service (value between 0 and 1)", false, 1, 1);
            Option streamingOpt = Option.GetLongOnly("stream", "If given the audio is streamed to the client", false, 0, 0);

            Options options = new Options(sourcesOpt, ifNonOpt, reloadOpt, clientOpt, serverOpt, playOpt,
                allShuffleOpt, searchShuffleOpt, onlySearchOpt, searchKeyOpt, serviceVolOpt, streamingOpt);
            OptionParseResult result = options.Parse(args);

            int port;
            float volume;
            OptionParsed parsed;

            if (result.TryGetFirstValidOptionParseds(serverOpt, out parsed) &&
                int.TryParse(parsed.Values[0], out port)) WithServer(port);

            if (result.TryGetFirstValidOptionParseds(clientOpt, out parsed))
            {
                string serverAddress = parsed.Values[0];

                if (parsed.Values.Count > 1 && int.TryParse(parsed.Values[1], out port)) WithClient(serverAddress, port);
                else WithClient(serverAddress);
            }

            if (result.HasValidOptionParseds(sourcesOpt))
            {
                WithMediaSources(result.GetValidOptionParseds(sourcesOpt).SelectMany(p => p.Values).ToArray());
            }

            if (result.TryGetFirstValidOptionParseds(allShuffleOpt, out parsed)) WithIsAllShuffle();
            if (result.TryGetFirstValidOptionParseds(searchShuffleOpt, out parsed)) WithIsSearchShuffle();
            if (result.TryGetFirstValidOptionParseds(onlySearchOpt, out parsed)) WithIsOnlySearch();
            if (result.TryGetFirstValidOptionParseds(searchKeyOpt, out parsed)) WithSearchKey(parsed.Values.FirstOrDefault());
            if (result.HasValidOptionParseds(ifNonOpt)) WithSetMediaIfNon();
            if (result.HasValidOptionParseds(reloadOpt)) WithReload();
            if (result.HasValidOptionParseds(playOpt)) WithPlay();

            if (result.TryGetFirstValidOptionParseds(serviceVolOpt, out parsed) &&
                float.TryParse(parsed.Values[0], out volume)) WithVolume(volume);

            if (result.HasValidOptionParseds(streamingOpt)) WithIsStreaming();

            return this;
        }

        public ServiceBuilder WithService(IAudioService service)
        {
            Service = service;

            return WithMediaSources(service.SourcePlaylist.FileMediaSources)
                .WithIsAllShuffle(service.SourcePlaylist.IsAllShuffle)
                .WithIsSearchShuffle(service.SourcePlaylist.IsSearchShuffle)
                .WithSearchKey(service.SourcePlaylist.SearchKey)
                //.WithPlay(service.PlayState == PlaybackState.Playing)
                .WithReload(false)
                .WithSetMediaIfNon(false)
                .WithVolume(service.Volume);
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

        public ServiceBuilder WithMediaSources(string[] mediaSources)
        {
            MediaSources = mediaSources;

            return this;
        }

        public ServiceBuilder WithSetMediaIfNon(bool value = true)
        {
            IfNon = value;

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

        public ServiceBuilder WithIsOnlySearch(bool? value = true)
        {
            IsOnlySearch = value;

            return this;
        }

        public ServiceBuilder WithSearchKey(string value)
        {
            SearchKey = value;

            return this;
        }

        public ServiceBuilder WithReload(bool value = true)
        {
            Reload = value;

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

        public ServiceBuilder WithPlayer(IWaveProviderPlayer player)
        {
            Player = player;

            return this;
        }

        public ICommunicator CreateCommunicator()
        {
            if (BuildServer) return CreateMqttServerCommunicator(ServerPort);
            if (BuildClient) return CreateMqttClientCommunicator(ServerAddress, ClientPort);

            return null;
        }

        public IServicePlayer CreateServicePlayer(IAudioService service)
        {
            if (BuildClient) return CreateAudioStreamPlayer(Player, service);
            return CreateAudioServicePlayer(Player, service);
        }

        public void CompleteService(IAudioService service)
        {
            bool setMediaSources = mediaSources != null && (!ifNon || !service.SourcePlaylist.FileMediaSources.ToNotNull().Any());
            if (setMediaSources && !mediaSources.BothNullOrSequenceEqual(service.SourcePlaylist.FileMediaSources))
            {
                service.SourcePlaylist.FileMediaSources = mediaSources;
            }
            else if (reload) service.SourcePlaylist.Reload();

            if (IsAllShuffle.HasValue) service.SourcePlaylist.IsAllShuffle = IsAllShuffle.Value;
            if (IsSearchShuffle.HasValue) service.SourcePlaylist.IsSearchShuffle = IsSearchShuffle.Value;
            if (SearchKey != null) service.SourcePlaylist.SearchKey = SearchKey;
            if (play.HasValue) service.PlayState = play.Value ? PlaybackState.Playing : PlaybackState.Paused;
            if (volume.HasValue) service.Volume = volume.Value;
        }

        protected virtual MqttClientCommunicator CreateMqttClientCommunicator(string serverAddress, int? port)
        {
            return new MqttClientCommunicator(serverAddress, port, helper);
        }

        protected virtual MqttServerCommunicator CreateMqttServerCommunicator(int port)
        {
            return new MqttServerCommunicator(port, helper);
        }

        protected virtual AudioStreamPlayer CreateAudioStreamPlayer(IWaveProviderPlayer player, IAudioService service)
        {
            return helper.CreateAudioStreamPlayer(player, service);
        }

        protected virtual AudioServicePlayer CreateAudioServicePlayer(IWaveProviderPlayer player, IAudioService service)
        {
            return helper.CreateAudioServicePlayer(player, service);
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
