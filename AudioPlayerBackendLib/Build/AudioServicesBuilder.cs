using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using AudioPlayerBackend.AudioLibrary.Database.Sql;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp;
using AudioPlayerBackend.AudioLibrary.LibraryRepo.Sqlite;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.Sqlite;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.OwnTcp;
using AudioPlayerBackend.FileSystem;
using AudioPlayerBackend.FileSystem.OwnTcp;
using AudioPlayerBackend.Player;
using AudioPlayerBackend.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StdOttStandard.Linq;

namespace AudioPlayerBackend.Build
{
    public enum BuildState { Init, Building, Starting, Completing, Finished }

    public class AudioServicesBuilder : INotifyPropertyChanged
    {
        private readonly AudioServicesBuildConfig config;
        private BuildState state;

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

        public BuildStatusToken<AudioServices> CompleteToken { get; }

        public string CommunicatorName
        {
            get
            {
                if (config.BuildServer)
                {
                    return $"Server: {config.ServerPort}";
                }

                if (config.BuildClient)
                {
                    return $"{config.ServerAddress?.Trim()} : {config.ClientPort}";
                }

                return null;
            }
        }

        public AudioServicesBuilder()
        {
            
        }

        public AudioServicesBuilder(AudioServicesBuildConfig config)
        {
            State = BuildState.Init;
            CompleteToken = new BuildStatusToken<AudioServices>();
            this.config = config;
        }

        public void Cancel()
        {
            CompleteToken.Cancel();
        }

        public void Settings()
        {
            CompleteToken.Settings();
        }

        public async void StartBuild(TimeSpan delayTime)
        {
            Logs.Log("Builder.StartBuild1");
            if (State != BuildState.Init) throw new InvalidOperationException("Build has already benn started: " + State);

            while (true)
            {
                Logs.Log("Builder.StartBuild2");
                CompleteToken.Reset();
                Logs.Log("Builder.StartBuild3");

                AudioServices audioServices = null;

                try
                {
                    State = BuildState.Building;
                    Logs.Log("Builder.StartBuild4");
                    audioServices = BuildAudioServices();
                    Logs.Log("Builder.StartBuild5");

                    State = BuildState.Starting;
                    Logs.Log("Builder.StartBuild6");
                    await audioServices.Start();
                    Logs.Log("Builder.StartBuild7");

                    State = BuildState.Completing;
                    Logs.Log("Builder.StartBuild8");
                    await CompleteServices(audioServices);
                    Logs.Log("Builder.StartBuild9");

                    if (CompleteToken.IsEnded.HasValue) return;

                    CompleteToken.End(BuildEndedType.Successful, audioServices);
                    Logs.Log("Builder.StartBuild10");
                }
                catch (Exception e)
                {
                    CompleteToken.Exception = e;

                    if (audioServices != null) await audioServices.Dispose();

                    if (CompleteToken.IsEnded.HasValue) return;

                    await Task.Delay(delayTime);
                    continue;
                }
                break;
            }

            State = BuildState.Finished;
        }

        private AudioServices BuildAudioServices()
        {
            IServiceProvider serviceProvider = BuildServiceProvider();

            IList<IAudioService> backgroundServices = new List<IAudioService>();
            IList<IAudioService> foregroundServices = new List<IAudioService>();
            IList<IAudioService> intensiveServices = new List<IAudioService>();

            if (config.BuildClient)
            {
                foregroundServices.Add(serviceProvider.GetService<IClientCommunicator>());
                foregroundServices.Add(serviceProvider.GetService<IPlaylistsRepo>());
                foregroundServices.Add(serviceProvider.GetService<ILibraryRepo>());
                //foregroundServices.Add(serviceProvider.GetService<IUpdateLibraryService>());
            }

            if (config.BuildStandalone || config.BuildServer)
            {
                // playlist repo must be before library repo
                // because the order in which the init sqls run is important
                backgroundServices.Add(serviceProvider.GetService<IPlaylistsRepo>());
                backgroundServices.Add(serviceProvider.GetService<ILibraryRepo>());
            }

            if (config.BuildStandalone || config.BuildServer)
            {
                backgroundServices.Add(serviceProvider.GetService<IPlayerService>());
            }

            if (config.BuildServer)
            {
                backgroundServices.Add(serviceProvider.GetService<IServerCommunicator>());
                backgroundServices.Add(serviceProvider.GetService<IServerLibraryRepoConnector>());
                backgroundServices.Add(serviceProvider.GetService<IServerPlaylistsRepoConnector>());
                backgroundServices.Add(serviceProvider.GetService<IServerUpdateLibraryServiceConnector>());
            }

            if (config.AutoUpdate && (config.BuildStandalone || config.BuildServer))
            {
                intensiveServices.Add(serviceProvider.GetService<AutoUpdateLibraryService>());
            }

            foregroundServices.Add(serviceProvider.GetService<ILibraryViewModel>());

            return new AudioServices(serviceProvider, backgroundServices, foregroundServices, intensiveServices);
        }

        private IServiceProvider BuildServiceProvider()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(config);

            if (config.BuildStandalone || config.BuildServer)
            {
                services.AddSingleton<ISqlExecuteService, SqliteExecuteService>();
                services.AddSingleton<ILibraryRepo, SqliteLibraryRepo>();
                services.AddSingleton<IPlaylistsRepo, SqlitePlaylistsRepo>();
            }
            else if (config.BuildClient)
            {
                services.AddSingleton<IClientCommunicator, OwnTcpClientCommunicator>();
                services.AddSingleton<ILibraryRepo, OwnTcpLibraryRepo>();
                services.AddSingleton<IPlaylistsRepo, OwnTcpPlaylistsRepo>();
                services.AddSingleton<IUpdateLibraryService, OwnTcpUpdateLibraryService>();
            }
            else throw new NotSupportedException("Mode not supported");

            if (config.BuildServer)
            {
                services.AddSingleton<IServerCommunicator, OwnTcpServerCommunicator>();
                services.AddSingleton<IServerLibraryRepoConnector, OwnTcpServerLibraryRepoConnector>();
                services.AddSingleton<IServerPlaylistsRepoConnector, OwnTcpServerPlaylistsRepoConnector>();
                services.AddSingleton<IServerUpdateLibraryServiceConnector, OwnTcpServerUpdateLibraryServiceConnector>();
            }

            services.AddTransient<IPlayerService, AudioPlayerService>();

            services.AddTransient<AutoUpdateLibraryService>();

            services.AddSingleton<ILibraryViewModel, LibraryViewModel>();
            services.AddTransient<IPlaylistViewModel, PlaylistViewModel>();
            services.AddTransient<ISongSearchViewModel, SongSearchViewModel>();

            foreach (ServiceDescriptor service in config.AdditionalServices.ToNotNull())
            {
                services.TryAdd(service);
            }

            return services.BuildServiceProvider();
        }

        private async Task CompleteServices(AudioServices audioServices)
        {
            ILibraryRepo libraryRepo = audioServices.GetLibraryRepo();

            if (config.Shuffle.HasValue)
            {
                Library library = await libraryRepo.GetLibrary();

                IPlaylistsRepo playlistsRepo = audioServices.GetPlaylistsRepo();
                foreach (PlaylistInfo playlist in library.Playlists)
                {
                    await playlistsRepo.SetShuffle(playlist.Id, config.Shuffle.Value);
                }
            }

            if (config.Play.HasValue) await libraryRepo.SetPlayState(config.Play.Value ? PlaybackState.Playing : PlaybackState.Paused);
            if (config.Volume.HasValue) await libraryRepo.SetVolume(config.Volume.Value);

            ILibraryViewModel libraryViewModel = audioServices.ServiceProvider.GetService<ILibraryViewModel>();
            ISongSearchViewModel songSearchViewModel = libraryViewModel.SongSearch;
            if (config.IsSearchShuffle.HasValue)
            {
                songSearchViewModel.IsSearchShuffle = config.IsSearchShuffle.Value;
            };
            if (config.SearchKey != null) songSearchViewModel.SearchKey = config.SearchKey;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
