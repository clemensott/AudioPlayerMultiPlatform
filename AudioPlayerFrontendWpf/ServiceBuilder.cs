using AudioPlayerBackendLib;
using NAudio.Wave;
using StdOttWpfLib;
using StdOttWpfLib.CommendlinePaser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerFrontendWpf
{
    public class ServiceBuilder : INotifyPropertyChanged
    {
        private bool ifNon, reload;
        private bool? isAllShuffle, isSearchShuffle, isOnlySearch, play, isStreaming;
        private int serverPort;
        private int? clientPort;
        private string searchKey, serverAddress;
        private float? volume, clientVolume;
        private string[] mediaSources;
        private IAudioExtended service;
        private IntPtr? windowHandler;

        public bool BuildStandalone { get; private set; }

        public bool BuildServer { get; private set; }

        public bool BuildClient { get; private set; }

        public bool IfNon
        {
            get { return ifNon; }
            set
            {
                if (value == ifNon) return;

                ifNon = value;
                OnPropertyChanged(nameof(IfNon));
            }
        }

        public bool Reload
        {
            get { return reload; }
            set
            {
                if (value == reload) return;

                reload = value;
                OnPropertyChanged(nameof(Reload));
            }
        }

        public bool? IsAllShuffle
        {
            get { return isAllShuffle; }
            set
            {
                if (value == isAllShuffle) return;

                isAllShuffle = value;
                OnPropertyChanged(nameof(IsAllShuffle));
            }
        }

        public bool? IsSearchShuffle
        {
            get { return isSearchShuffle; }
            set
            {
                if (value == isSearchShuffle) return;

                isSearchShuffle = value;
                OnPropertyChanged(nameof(IsSearchShuffle));
            }
        }

        public bool? IsOnlySearch
        {
            get { return isOnlySearch; }
            set
            {
                if (value == isOnlySearch) return;

                isOnlySearch = value;
                OnPropertyChanged(nameof(IsOnlySearch));
            }
        }

        public string SearchKey
        {
            get { return searchKey; }
            set
            {
                if (value == searchKey) return;

                searchKey = value;
                OnPropertyChanged(nameof(SearchKey));
            }
        }

        public bool? Play
        {
            get { return play; }
            set
            {
                if (value == play) return;

                play = value;
                OnPropertyChanged(nameof(Play));
            }
        }

        public bool? IsStreaming
        {
            get { return isStreaming; }
            set
            {
                if (value == isStreaming) return;

                isStreaming = value;
                OnPropertyChanged(nameof(IsStreaming));
            }
        }

        public int ServerPort
        {
            get { return serverPort; }
            set
            {
                if (value == serverPort) return;

                serverPort = value;
                OnPropertyChanged(nameof(ServerPort));
            }
        }

        public int? ClientPort
        {
            get { return clientPort; }
            set
            {
                if (value == clientPort) return;

                clientPort = value;
                OnPropertyChanged(nameof(ClientPort));
            }
        }

        public string ServerAddress
        {
            get { return serverAddress; }
            set
            {
                if (value == serverAddress) return;

                serverAddress = value;
                OnPropertyChanged(nameof(ServerAddress));
            }
        }

        public float? Volume
        {
            get { return volume; }
            set
            {
                if (value == volume) return;

                volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        public float? ClientVolume
        {
            get { return clientVolume; }
            set
            {
                if (value == clientVolume) return;

                clientVolume = value;
                OnPropertyChanged(nameof(ClientVolume));
            }
        }

        public string[] MediaSources
        {
            get { return mediaSources; }
            set
            {
                if (value.BothNullOrSequenceEqual(mediaSources)) return;

                mediaSources = value;
                OnPropertyChanged(nameof(MediaSources));
            }
        }

        public IAudioExtended Service
        {
            get { return service; }
            set
            {
                if (value == service) return;

                service = value;
                OnPropertyChanged(nameof(Service));
            }
        }

        public ServiceBuilder()
        {
            WithStandalone();
        }

        public ServiceBuilder WithArgs(IEnumerable<string> args)
        {
            Option clientOpt = new Option("c", "client",
                "Starts the app as client with the following server adresse and port", false, 2, 1);
            Option serverOpt = new Option("s", "server", "Starts the app as server with the following port", false, 1, 1);
            Option sourcesOpt = new Option("m", "media-sources", "Files and diretories to play", false, -1, 0);
            Option ifNonOpt = new Option("i", "if-non",
                "If given the Media sources are only used if there are non", false, 0, 0);
            Option reloadOpt = new Option("r", "reload", "Forces to reload", false, 0, 0);
            Option allShuffleOpt = Option.GetLongOnly("all-shuffle", "Shuffles all songs.", false, 0, 0);
            Option searchShuffleOpt = Option.GetLongOnly("search-shuffle", "Shuffles all songs.", false, 0, 0);
            Option onlySearchOpt = Option.GetLongOnly("only-search", "Shuffles all songs.", false, 0, 0);
            Option searchKeyOpt = Option.GetLongOnly("search-key", "Shuffles all songs.", false, 1, 0);
            Option playOpt = new Option("p", "play", "Starts playback on startup", false, 0, 0);
            Option serviceVolOpt = new Option("v", "volume", "The volume of service (value between 0 and 1)", false, 1, 1);
            Option streamingOpt = Option.GetLongOnly("stream", "If given the audio is streamed to the client", false, 0, 0);
            Option clientVolOpt = Option.GetLongOnly("client-volume",
                "The volume of client for streaming (value between 0 and 1)", false, 1, 1);

            Options options = new Options(sourcesOpt, ifNonOpt, reloadOpt, clientOpt, serverOpt, playOpt);
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

            if (result.TryGetFirstValidOptionParseds(clientVolOpt, out parsed) &&
                float.TryParse(parsed.Values[0], out volume)) WithClientVolume(volume);

            return this;
        }

        public ServiceBuilder WithService(IAudioExtended service)
        {
            if (service is IMqttAudioService) WithService(service as IMqttAudioService);
            else if (service is IMqttAudioClient) WithService(service as IMqttAudioClient);

            Service = service;

            return WithMediaSources(service.MediaSources)
                .WithIsAllShuffle(service.IsAllShuffle)
                .WithIsSearchShuffle(service.IsSearchShuffle)
                .WithIsOnlySearch(service.IsOnlySearch)
                .WithSearchKey(service.SearchKey)
                .WithPlay(service.PlayState == PlaybackState.Playing)
                .WithReload(false)
                .WithSetMediaIfNon(false)
                .WithVolume(service.Volume)
                .WithWindowHandler(service.WindowHandle);
        }

        private ServiceBuilder WithService(IMqttAudioClient client)
        {
            return WithClient(client.ServerAddress, client.Port)
               .WithIsStreaming(client.IsStreaming)
               .WithClientVolume(client.ClientVolume);
        }

        private ServiceBuilder WithService(IMqttAudioService server)
        {
            return WithServer(server.Port);
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

        private ServiceBuilder WithIsAllShuffle(bool? value = true)
        {
            IsAllShuffle = value;

            return this;
        }

        private ServiceBuilder WithIsSearchShuffle(bool? value = true)
        {
            IsSearchShuffle = value;

            return this;
        }

        private ServiceBuilder WithIsOnlySearch(bool? value = true)
        {
            IsOnlySearch = value;

            return this;
        }

        private ServiceBuilder WithSearchKey(string value)
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

        public ServiceBuilder WithVolume(float volume)
        {
            Volume = volume;

            return this;
        }

        public ServiceBuilder WithIsStreaming(bool? value = true)
        {
            IsStreaming = value;

            return this;
        }

        public ServiceBuilder WithClientVolume(float? volume)
        {
            ClientVolume = volume;

            return this;
        }

        public ServiceBuilder WithWindowHandler(IntPtr? windowHandler = null)
        {
            this.windowHandler = windowHandler;

            return this;
        }

        public async Task<IAudioExtended> Build()
        {
            IAudioExtended service;

            if (BuildServer)
            {
                IMqttAudioService server = Service as IMqttAudioService;

                if (server != null && (ServerPort != server.Port || windowHandler != server.WindowHandle))
                {
                    if (server.IsOpen) await server.CloseAsync();

                    server = null;
                }

                if (server == null)
                {
                    server = new MqttAudioService(ServerPort, windowHandler);
                }

                if (!server.IsOpen) await server.OpenAsync();

                service = server;
            }
            else if (BuildClient)
            {
                IMqttAudioClient client = Service as IMqttAudioClient;

                if (client != null && (ServerAddress != client.ServerAddress ||
                    ClientPort != client.Port || windowHandler != client.WindowHandle))
                {
                    client = null;
                }

                if (client == null)
                {
                    client = clientPort.HasValue ? new MqttAudioClient(serverAddress, clientPort.Value, windowHandler)
                        : new MqttAudioClient(serverAddress, windowHandler);
                }

                if (!client.IsOpen) await client.OpenAsync();

                if (isStreaming.HasValue) client.IsStreaming = isStreaming.Value;
                if (clientVolume.HasValue) client.ClientVolume = clientVolume.Value;

                service = client;
            }
            else
            {
                service = Service == null || Service is IMqttAudio ? new AudioService(windowHandler) : Service;

                if (mediaSources == null) mediaSources = new string[] { Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) };
            }

            bool setMediaSources = mediaSources != null && (!ifNon || !service.MediaSources.ToNotNull().Any());

            if (setMediaSources && !mediaSources.BothNullOrSequenceEqual(service.MediaSources)) service.MediaSources = mediaSources;
            else if (reload) service.Reload();

            if (!reload && Service != null && ContainsSameSongs(Service.AllSongsShuffled, service.AllSongsShuffled))
            {
                service.AllSongsShuffled = Service.AllSongsShuffled;
                service.CurrentSong = Service.CurrentSong;
            }

            if (IsAllShuffle.HasValue) service.IsAllShuffle = IsAllShuffle.Value;
            if (IsSearchShuffle.HasValue) service.IsSearchShuffle = IsSearchShuffle.Value;
            if (IsOnlySearch.HasValue) service.IsOnlySearch = IsOnlySearch.Value;
            if (SearchKey != null) service.SearchKey = SearchKey;
            if (play.HasValue) service.PlayState = play.Value ? PlaybackState.Playing : PlaybackState.Paused;
            if (volume.HasValue) service.Volume = volume.Value;

            return service;
        }

        private bool ContainsSameSongs(IEnumerable<Song> enum1, IEnumerable<Song> enum2)
        {
            if (enum1 == enum2) return true;
            if (enum1 == null || enum2 == null) return false;

            return enum1.All(s => enum2.Contains(s));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
