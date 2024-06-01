using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.OwnTcp;
using AudioPlayerBackend.AudioLibrary.Sqlite;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.OwnTcp;
using AudioPlayerBackend.Data;
using AudioPlayerBackend.FileSystem;
using AudioPlayerBackend.Player;
using AudioPlayerBackend.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StdOttStandard.Linq;

namespace AudioPlayerBackend.Build
{
    public enum BuildState { Init, OpenCommunicator, SyncCommunicator, SendCommands, CreatePlayer, CompleteSerivce, Finished }

    public class AudioServicesBuilder : INotifyPropertyChanged
    {
        private readonly IAudioCreateService audioCreateService;
        private readonly AudioServicesBuildConfig config;
        private bool sendCommandsDirect, sendToggle;
        private int songOffset;
        private BuildState state;
        private PlaybackState? sendPlayState;
        private ICommunicator communicator;
        private IAudioService service;
        private IPlayerService servicePlayer;

        public bool SendToggle
        {
            get => sendToggle;
            private set
            {
                if (value == sendToggle) return;

                sendToggle = value;
                OnPropertyChanged(nameof(SendToggle));
            }
        }

        public int SongOffset
        {
            get => songOffset;
            private set
            {
                if (value == songOffset) return;

                songOffset = value;
                OnPropertyChanged(nameof(SongOffset));
            }
        }

        public PlaybackState? SendPlayState
        {
            get => sendPlayState;
            private set
            {
                if (value == sendPlayState) return;

                sendPlayState = value;
                OnPropertyChanged(nameof(SendPlayState));
            }
        }

        public BuildState State
        {
            get => state;
            set
            {
                if (value == state) return;

                state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public ICommunicator Communicator
        {
            get => communicator;
            set
            {
                if (value == communicator) return;

                communicator = value;
                OnPropertyChanged(nameof(Communicator));
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

        public IPlayerService ServicePlayer
        {
            get => servicePlayer;
            set
            {
                if (value == servicePlayer) return;

                servicePlayer = value;
                OnPropertyChanged(nameof(ServicePlayer));
            }
        }

        public BuildStatusToken<ICommunicator> CommunicatorToken { get; }

        public BuildStatusToken<IAudioService> SyncToken { get; }

        public BuildStatusToken<IPlayerService> PlayerToken { get; }

        public BuildStatusToken<AudioServices> CompleteToken { get; }

        public AudioServicesBuilder(AudioServicesBuildConfig config)
        {
            audioCreateService = AudioPlayerServiceProvider.Current.GetAudioCreateService();
            State = BuildState.Init;
            CommunicatorToken = new BuildStatusToken<ICommunicator>();
            SyncToken = new BuildStatusToken<IAudioService>();
            PlayerToken = new BuildStatusToken<IPlayerService>();
            CompleteToken = new BuildStatusToken<AudioServices>();
            this.config = config;
        }

        public void Cancel()
        {
            CommunicatorToken.Cancel();
            SyncToken.Cancel();
            PlayerToken.Cancel();
            CompleteToken.Cancel();
        }

        public void Settings()
        {
            CommunicatorToken.Settings();
            SyncToken.Settings();
            PlayerToken.Settings();
            CompleteToken.Settings();
        }

        public static AudioServicesBuilder Build(AudioServicesBuildConfig config, TimeSpan delayTime)
        {
            AudioServicesBuilder build = new AudioServicesBuilder(config);
            build.StartBuild(delayTime);

            return build;
        }

        public async void StartBuild(TimeSpan delayTime)
        {
            if (State != BuildState.Init) throw new InvalidOperationException("Build has already benn started: " + State);

            try
            {
                while (true)
                {
                    CommunicatorToken.Reset();
                    SyncToken.Reset();
                    PlayerToken.Reset();
                    CompleteToken.Reset();

                    try
                    {
                        State = BuildState.CompleteSerivce;

                        AudioServices audioServices = BuildAudioServices();

                        await CompleteServices(audioServices);

                        if (CompleteToken.IsEnded.HasValue) return;

                        CompleteToken.End(BuildEndedType.Successful, audioServices);
                    }
                    catch (Exception e)
                    {
                        CompleteToken.Exception = e;

                        if (CompleteToken.IsEnded.HasValue) return;

                        await Task.Delay(delayTime);
                        continue;
                    }
                    break;
                }

                State = BuildState.Finished;
            }
            finally
            {
                if (Communicator != null && State != BuildState.Finished) await Communicator.CloseAsync();
            }
        }

        private AudioServices BuildAudioServices()
        {
            IServiceProvider serviceProvider = BuildServiceProvider();

            IList<IAudioService> serviceList = new List<IAudioService>();
            if (config.BuildStandalone || config.BuildServer)
            {
                serviceList.Add(serviceProvider.GetService<IPlayerService>());
            }

            if (config.BuildServer)
            {
                serviceList.Add(serviceProvider.GetService<IServerCommunicator>());
            }

            if (config.BuildStandalone || config.BuildServer)
            {
                serviceList.Add(serviceProvider.GetService<AutoUpdateLibraryService>());
            }

            serviceList.Add(serviceProvider.GetService<ILibraryViewModel>());

            return new AudioServices(serviceProvider, serviceList);
        }

        private IServiceProvider BuildServiceProvider()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(config);

            if (config.BuildStandalone || config.BuildServer)
            {
                services.AddSingleton<ILibraryRepo, SqliteLibraryRepo>();
                services.AddSingleton<IPlaylistsRepo, SqlitePlaylistsRepo>();
            }
            else if (config.BuildClient)
            {
                services.AddSingleton<ILibraryRepo, OwnTcpLibraryRepo>();
                services.AddSingleton<IPlaylistsRepo, OwnTcpPlaylistsRepo>();
            }
            else throw new NotSupportedException("Mode not supported");

            services.AddSingleton<IServerCommunicator, OwnTcpServerCommunicator>();
            services.AddSingleton<IClientCommunicator, OwnTcpClientCommunicator>();

            services.AddTransient<IPlayerService, AudioPlayerService>();

            services.AddTransient<UpdateLibraryService>();
            services.AddTransient<AutoUpdateLibraryService>();

            services.AddTransient<ILibraryViewModel, LibraryViewModel>();
            services.AddTransient<IPlaylistViewModel, PlaylistViewModel>();
            services.AddTransient<ISongSearchViewModel, SongSearchViewModel>();

            foreach (ServiceDescriptor service in config.AdditionalServices.ToNotNull())
            {
                services.Replace(service);
            }

            return services.BuildServiceProvider();
        }

        public async Task CompleteServices(AudioServices audioServices)
        {
            ILibraryRepo libraryRepo = audioServices.ServiceProvider.GetService<ILibraryRepo>();
            IPlaylistsRepo playlistsRepo = audioServices.ServiceProvider.GetService<IPlaylistsRepo>();

            if (config.Shuffle.HasValue)
            {
                Library library = await libraryRepo.GetLibrary();

                foreach (PlaylistInfo playlist in library.Playlists)
                {
                    await playlistsRepo.SendShuffleChange(playlist.Id, config.Shuffle.Value);
                }
            }

            if (config.Play.HasValue) await libraryRepo.SendPlayStateChange(config.Play.Value ? PlaybackState.Playing : PlaybackState.Paused);
            if (config.Volume.HasValue) await libraryRepo.SendVolumeChange(config.Volume.Value);

            ILibraryViewModel libraryViewModel = audioServices.ServiceProvider.GetService<ILibraryViewModel>();
            ISongSearchViewModel songSearchViewModel = libraryViewModel.SongSearuch;
            if (config.IsSearchShuffle.HasValue)
            {
                if (config.IsSearchShuffle.Value) songSearchViewModel.Enable();
                else songSearchViewModel.Disable();
            };
            if (config.SearchKey != null) songSearchViewModel.SearchKey = config.SearchKey;
        }

        public async Task SetNextSong(int offset = 1)
        {
            if (sendCommandsDirect)
            {
                for (int i = 0; i < offset; i++)
                {
                    await Communicator.NextSong();
                }
            }
            else SongOffset += offset;
        }

        public async Task SetPreviousSong(int offset = 1)
        {
            if (sendCommandsDirect)
            {
                for (int i = 0; i < offset; i++)
                {
                    await Communicator.PreviousSong();
                }
            }
            else SongOffset -= offset;
        }

        public async Task SetPlayState(PlaybackState? state)
        {
            SendToggle = false;
            SendPlayState = state;

            if (sendCommandsDirect)
            {
                if (state == PlaybackState.Playing) await Communicator.PlaySong();
                else if (state == PlaybackState.Paused) await Communicator.PauseSong();
            }
        }

        public async Task SetToggle(bool value = true)
        {
            SendToggle = value;
            SendPlayState = null;

            if (sendCommandsDirect)
            {
                if (SendToggle)
                {
                    await Communicator.ToggleSong();
                    SendToggle = false;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
