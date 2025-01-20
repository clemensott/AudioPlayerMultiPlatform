using System;
using StdOttStandard.CommandlineParser;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using StdOttStandard.Linq;
using Microsoft.Extensions.DependencyInjection;
using AudioPlayerBackend.ViewModels;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using System.IO;

namespace AudioPlayerBackend.Build
{
    public class AudioServicesBuildConfig : INotifyPropertyChanged
    {
        private bool autoUpdate;
        private bool? isSearchShuffle, play;
        private OrderType? shuffle;
        private int serverPort;
        private int? clientPort;
        private string searchKey, serverAddress, dataFilePath;
        private float? volume;
        private FileMediaSourceRootInfo[] defaultUpdateRoots;
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

        public FileMediaSourceRootInfo[] DefaultUpdateRoots
        {
            get => defaultUpdateRoots;
            set
            {
                if (value == defaultUpdateRoots) return;

                defaultUpdateRoots = value;
                OnPropertyChanged(nameof(DefaultUpdateRoots));
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
            Option clientOpt = new Option("c", "client", "Starts the app as client with the following server address and port", false, 2, 1);
            Option serverOpt = new Option("s", "server", "Starts the app as server with the following port", false, 1, 1);
            Option sourcesOpt = new Option("m", "media-sources", "Files and directories to play", false, -1, 0);
            Option orderSongsOpt = Option.GetLongOnly("order", "Order type for all playlists.", false, 1);
            Option searchShuffleOpt = Option.GetLongOnly("search-shuffle", "Shuffles all songs.", false, 0, 0);
            Option searchKeyOpt = Option.GetLongOnly("search-key", "Shuffles all songs.", false, 1, 0);
            Option playOpt = new Option("p", "play", "Starts playback on startup", false, 0, 0);
            Option serviceVolOpt = new Option("v", "volume", "The volume of service (value between 0 and 1)", false, 1, 1);
            Option dataFileOpt = new Option("d", "data-file", "Filepath to where to read and write data.", false, 1, 1);
            Option autoUpdateOpt = new Option("a", "auto-update", "Enable auto update of library and its playlists.", false, 0, 0);
            // Examples
            //   --default-update-sources Songs+Folders KnownFolder MyMusic
            //   -dus Songs Path D:\Music
            Option defaultUpdateRootsOpt = new Option("dus", "default-update-sources", "Filepaths to source roots that create playlists.", false, 3, 3);

            Options options = new Options(sourcesOpt, clientOpt, serverOpt, playOpt,
                orderSongsOpt, searchShuffleOpt, searchKeyOpt, serviceVolOpt, dataFileOpt, autoUpdateOpt, defaultUpdateRootsOpt);
            OptionParseResult result = options.Parse(args);

            if (result.TryGetFirstValidOptionParseds(serverOpt, out parsed))
            {
                WithServer(int.Parse(parsed.Values[0]));
            }

            if (result.TryGetFirstValidOptionParseds(clientOpt, out parsed))
            {
                if (parsed.Values.Count > 1) WithClient(parsed.Values[0], int.Parse(parsed.Values[1]));
                else WithClient(parsed.Values[0]);
            }

            if (result.TryGetFirstValidOptionParseds(orderSongsOpt, out parsed)) WithShuffle((OrderType)Enum.Parse(typeof(OrderType), parsed.Values[0]));
            if (result.TryGetFirstValidOptionParseds(searchShuffleOpt, out parsed)) WithIsSearchShuffle();
            if (result.TryGetFirstValidOptionParseds(searchKeyOpt, out parsed)) WithSearchKey(parsed.Values.FirstOrDefault());
            if (result.HasValidOptionParseds(playOpt)) WithPlay();

            if (result.TryGetFirstValidOptionParseds(serviceVolOpt, out parsed)) WithVolume(float.Parse(parsed.Values[0]));

            if (result.TryGetFirstValidOptionParseds(dataFileOpt, out parsed)) WithDateFilePath(parsed.Values[0]);

            if (result.HasValidOptionParseds(autoUpdateOpt)) WithAutoUpdate();
            if (result.HasValidOptionParseds(defaultUpdateRootsOpt))
            {
                IEnumerable<FileMediaSourceRootInfo> defaultRoots = result.GetValidOptionParseds(defaultUpdateRootsOpt).Select(option =>
                {
                    FileMediaSourceRootUpdateType updateType = option.Values[0].Split('+')
                        .Select(raw => (FileMediaSourceRootUpdateType)Enum.Parse(typeof(FileMediaSourceRootUpdateType), raw, true))
                        .Aggregate(FileMediaSourceRootUpdateType.None, (sum, item) => sum | item);
                    FileMediaSourceRootPathType pathType = (FileMediaSourceRootPathType)Enum
                        .Parse(typeof(FileMediaSourceRootPathType), option.Values[1], true);
                    string path = option.Values[2];
                    string name = Path.GetFileName(path);

                    return new FileMediaSourceRootInfo(updateType, name, pathType, path);
                });
                WithDefaultUpdateRoots(defaultRoots.ToArray());
            }

            return this;
        }

        public AudioServicesBuildConfig WithService(ILibraryViewModel viewModel)
        {
            return WithShuffle(null)
                .WithIsSearchShuffle(viewModel.SongSearch.IsSearchShuffle)
                .WithSearchKey(viewModel.SongSearch.SearchKey)
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

        public AudioServicesBuildConfig WithDateFilePath(string path)
        {
            DataFilePath = path;

            return this;
        }

        public AudioServicesBuildConfig WithVolume(float? volume)
        {
            Volume = volume;

            return this;
        }

        public AudioServicesBuildConfig WithAutoUpdate(bool value = true)
        {
            AutoUpdate = value;

            return this;
        }

        public AudioServicesBuildConfig WithDefaultUpdateRoots(FileMediaSourceRootInfo[] roots)
        {
            DefaultUpdateRoots = roots;

            return this;
        }

        private ServiceCollection CloneAdditionalServices()
        {
            if (AdditionalServices == null) return null;

            var clone = new ServiceCollection();
            foreach (ServiceDescriptor service in additionalServices)
            {
                clone.TryAdd(service);
            }

            return clone;
        }

        public AudioServicesBuildConfig Clone()
        {
            return new AudioServicesBuildConfig()
            {
                BuildClient = BuildClient,
                BuildServer = BuildServer,
                BuildStandalone = BuildStandalone,
                AutoUpdate = AutoUpdate,
                DefaultUpdateRoots = DefaultUpdateRoots?.ToArray(),
                AdditionalServices = CloneAdditionalServices(),
                ClientPort = ClientPort,
                DataFilePath = DataFilePath,
                Shuffle = Shuffle,
                IsSearchShuffle = IsSearchShuffle,
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
