using System;
using StdOttStandard.CommandlineParser;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using StdOttStandard.Linq;
using Microsoft.Extensions.DependencyInjection;
using AudioPlayerBackend.ViewModels;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;

namespace AudioPlayerBackend.Build
{
    public class AudioServicesBuildConfig : INotifyPropertyChanged
    {
        private bool autoUpdate;
        private bool? isSearchShuffle, play, isStreaming;
        private OrderType? shuffle;
        private int serverPort;
        private int? clientPort;
        private string searchKey, serverAddress, dataFilePath;
        private float? volume;
        private CommunicatorProtocol communicatorProtocol;
        private string[] autoUpdateRoots;
        private ServiceCollection additionalServices;

        public bool BuildStandalone { get; private set; }

        public bool BuildServer { get; private set; }

        public bool BuildClient { get; private set; }

        public bool AutoUpdate
        {
            get => autoUpdate;
            set
            {
                if (value == autoUpdate) return;

                autoUpdate = value;
                OnPropertyChanged(nameof(AutoUpdate));
            }
        }

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

        public OrderType? Shuffle
        {
            get => shuffle;
            set
            {
                if (value == shuffle) return;

                shuffle = value;
                OnPropertyChanged(nameof(Shuffle));
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

        // TODO: make use of this
        public string[] AutoUpdateRoots
        {
            get => autoUpdateRoots;
            set
            {
                if (value == autoUpdateRoots) return;

                autoUpdateRoots = value;
                OnPropertyChanged(nameof(AutoUpdateRoots));
            }
        }

        public ServiceCollection AdditionalServices
        {
            get => additionalServices;
            set
            {
                if (value == additionalServices) return;

                additionalServices = value;
                OnPropertyChanged(nameof(AdditionalServices));
            }
        }

        public AudioServicesBuildConfig()
        {
            additionalServices = new ServiceCollection();

            WithStandalone();
        }

        public AudioServicesBuildConfig WithArgs(IEnumerable<string> args)
        {
            OptionParsed parsed;
            Option clientOpt = new Option("c", "client", "Starts the app as client with the following server address and port", false, 3, 2);
            Option serverOpt = new Option("s", "server", "Starts the app as server with the following port", false, 2, 2);
            Option sourcesOpt = new Option("m", "media-sources", "Files and directories to play", false, -1, 0);
            Option orderSongsOpt = Option.GetLongOnly("order", "Order type for all playlists.", false, 1);
            Option searchShuffleOpt = Option.GetLongOnly("search-shuffle", "Shuffles all songs.", false, 0, 0);
            Option searchKeyOpt = Option.GetLongOnly("search-key", "Shuffles all songs.", false, 1, 0);
            Option playOpt = new Option("p", "play", "Starts playback on startup", false, 0, 0);
            Option serviceVolOpt = new Option("v", "volume", "The volume of service (value between 0 and 1)", false, 1, 1);
            Option streamingOpt = Option.GetLongOnly("stream", "If given the audio is streamed to the client", false, 0, 0);
            Option dataFileOpt = new Option("d", "data-file", "Filepath to where to read and write data.", false, 1, 1);
            Option autoUpdateOpt = new Option("a", "auto-update", "Enable auto update of library and its playlists.", false, 0, 0);
            Option autoUpdateRootsOpt = Option.GetLongOnly("auto-update-sources", "Filepaths to source roots that create playlists.", false, 1, 1);

            Options options = new Options(sourcesOpt, clientOpt, serverOpt, playOpt,
                orderSongsOpt, searchShuffleOpt, searchKeyOpt, serviceVolOpt, streamingOpt, dataFileOpt, autoUpdateOpt, autoUpdateRootsOpt);
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

            if (result.TryGetFirstValidOptionParseds(orderSongsOpt, out parsed)) WithShuffle((OrderType)Enum.Parse(typeof(OrderType), parsed.Values[0]));
            if (result.TryGetFirstValidOptionParseds(searchShuffleOpt, out parsed)) WithIsSearchShuffle();
            if (result.TryGetFirstValidOptionParseds(searchKeyOpt, out parsed)) WithSearchKey(parsed.Values.FirstOrDefault());
            if (result.HasValidOptionParseds(playOpt)) WithPlay();

            if (result.TryGetFirstValidOptionParseds(serviceVolOpt, out parsed)) WithVolume(float.Parse(parsed.Values[0]));

            if (result.HasValidOptionParseds(streamingOpt)) WithIsStreaming();

            if (result.TryGetFirstValidOptionParseds(dataFileOpt, out parsed)) DataFilePath = parsed.Values[0];

            if (result.HasValidOptionParseds(autoUpdateOpt)) WithAutoUpdate();
            if (result.TryGetFirstValidOptionParseds(autoUpdateRootsOpt, out parsed)) WithAutoUpdateRoots(parsed.Values.ToArray());

            return this;
        }

        public AudioServicesBuildConfig WithService(ILibraryViewModel viewModel)
        {
            //return WithShuffle(GetSharedValueOrNull(viewModel.GetAllPlaylists().Select(p => p.Shuffle)))
            return WithShuffle(null)
                .WithIsSearchShuffle(viewModel.SongSearch.IsSearchShuffle)
                .WithSearchKey(viewModel.SongSearch.SearchKey)
                //.WithPlay(service.PlayState == PlaybackState.Playing)
                .WithVolume((float)viewModel.Volume);
        }

        private static T? GetSharedValueOrNull<T>(IEnumerable<T> values) where T : struct
        {
            T[] distinctValues = values.ToNotNull().Distinct().ToArray();
            return distinctValues.Length == 1 ? (T?)distinctValues[0] : null;
        }

        public AudioServicesBuildConfig WithStandalone()
        {
            BuildStandalone = true;
            BuildServer = false;
            BuildClient = false;

            return this;
        }

        public AudioServicesBuildConfig WithServer(int port)
        {
            BuildStandalone = false;
            BuildServer = true;
            BuildClient = false;

            return WithServerPort(port);
        }

        public AudioServicesBuildConfig WithCommunicatorProtocol(CommunicatorProtocol communicatorProtocol)
        {
            CommunicatorProtocol = communicatorProtocol;
            return this;
        }

        public AudioServicesBuildConfig WithMqtt()
        {
            CommunicatorProtocol = CommunicatorProtocol.MQTT;
            return this;
        }

        public AudioServicesBuildConfig WithOwnTcp()
        {
            CommunicatorProtocol = CommunicatorProtocol.OwnTCP;
            return this;
        }

        public AudioServicesBuildConfig WithServerPort(int port)
        {
            ServerPort = port;

            return this;
        }

        public AudioServicesBuildConfig WithClient(string serverAddress, int? port = null)
        {
            BuildStandalone = false;
            BuildServer = false;
            BuildClient = true;

            return WithServerAddress(serverAddress).WithClientPort(port);
        }

        public AudioServicesBuildConfig WithServerAddress(string serverAddress)
        {
            ServerAddress = serverAddress;

            return this;
        }

        public AudioServicesBuildConfig WithClientPort(int? port)
        {
            ClientPort = port;

            return this;
        }

        public AudioServicesBuildConfig WithShuffle(OrderType? value)
        {
            Shuffle = value;

            return this;
        }

        public AudioServicesBuildConfig WithIsSearchShuffle(bool? value = true)
        {
            IsSearchShuffle = value;

            return this;
        }

        public AudioServicesBuildConfig WithSearchKey(string value)
        {
            SearchKey = value;

            return this;
        }

        public AudioServicesBuildConfig WithPlay(bool? value = true)
        {
            Play = value;

            return this;
        }

        public AudioServicesBuildConfig WithVolume(float? volume)
        {
            Volume = volume;

            return this;
        }

        public AudioServicesBuildConfig WithIsStreaming(bool? value = true)
        {
            IsStreaming = value;

            return this;
        }

        public AudioServicesBuildConfig WithAutoUpdate(bool value = true)
        {
            AutoUpdate = value;

            return this;
        }

        public AudioServicesBuildConfig WithAutoUpdateRoots(string[] roots)
        {
            AutoUpdateRoots = roots;

            return this;
        }

        public AudioServicesBuildConfig Clone()
        {
            return new AudioServicesBuildConfig()
            {
                BuildClient = BuildClient,
                BuildServer = BuildServer,
                BuildStandalone = BuildStandalone,
                ClientPort = ClientPort,
                CommunicatorProtocol = CommunicatorProtocol,
                DataFilePath = DataFilePath,
                Shuffle = Shuffle,
                IsSearchShuffle = IsSearchShuffle,
                IsStreaming = IsStreaming,
                Play = Play,
                SearchKey = SearchKey,
                ServerAddress = ServerAddress,
                ServerPort = ServerPort,
                Volume = Volume,
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
