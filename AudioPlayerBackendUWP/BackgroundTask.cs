using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.FileSystem;
using AudioPlayerBackend.Player;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using AudioPlayerBackendUwpLib.Join;
using AudioPlayerBackendUwpLib;
using AudioPlayerBackendUwpLib.Extensions;
using System.Threading;
using AudioPlayerBackend.Build.Repo;
using StdOttStandard;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.Communication;
using AudioPlayerBackendUWP.Join;

namespace AudioPlayerBackendUWP
{
    public sealed class BackgroundTask : IBackgroundTask
    {
        private static readonly TimeSpan inativeTime = TimeSpan.FromMinutes(10);

        private bool stop;
        private IBackgroundTaskInstance instance;
        private BackgroundTaskDeferral deferral;
        private SemaphoreSlim runSem;
        private ResetTimer closeTimer;
        private PlaybackState playState;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            stop = false;
            instance = taskInstance;
            deferral = taskInstance.GetDeferral();
            runSem = new SemaphoreSlim(0, 1);
            closeTimer = ResetTimer.Start(inativeTime);
            playState = PlaybackState.Paused;

            instance.Canceled += Instance_Canceled;
            closeTimer.RanDown += CloseTimer_RanDown;

            await BuildAudioServices();
        }

        private async Task BuildAudioServices()
        {
            while (!stop)
            {
                AudioServicesBuildConfig config = await LoadConfig();

                if (config.BuildClient) break;

                AudioServicesBuilder builder = new AudioServicesBuilder(config);
                builder.StartBuild(TimeSpan.FromMilliseconds(200));

                AudioServices audioServices = await builder.CompleteToken.ResultTask;
                Subscribe(audioServices);

                await runSem.WaitAsync();

                Unsubscribe(audioServices);
                await audioServices.Stop();
            }

            deferral.Complete();
        }

        private static async Task<AudioServicesBuildConfig> LoadConfig()
        {
            AudioServicesBuildConfig config = new AudioServicesBuildConfig()
             .WithAutoUpdate()
             .WithDefaultUpdateRoots(new FileMediaSourceRootInfo[]
             {
                    new FileMediaSourceRootInfo(
                        FileMediaSourceRootUpdateType.Songs | FileMediaSourceRootUpdateType.Folders,
                        KnownFolders.MusicLibrary.DisplayName,
                        FileMediaSourceRootPathType.KnownFolder,
                        KnownFolderId.MusicLibrary.ToString()
                 ),
             })
             .WithDateFilePath("library.db");

            config.AdditionalServices.TryAddSingleton<IPlayer, Player>();
            config.AdditionalServices.TryAddSingleton<IFileSystemService, FileSystemService>();
            config.AdditionalServices.TryAddSingleton<IInvokeDispatcherService, InvokeDispatcherService>();
            config.AdditionalServices.TryAddSingleton<IUpdateLibraryService, UpdateLibraryService>();

            ServiceProfile? profile = await ServiceProfile.Load();
            if (profile.HasValue) config.WithServiceProfile(profile.Value);

            if (config.BuildStandalone)
            {
                config.WithServer(Settings.Current.BackgroundTaskPort);
            }
            else if (config.BuildServer)
            {
                Settings.Current.BackgroundTaskPort = config.ServerPort;
            }

            return config;
        }

        private void Subscribe(AudioServices audioServices)
        {
            if (audioServices == null) return;

            IAudioServicesRepo audioServicesRepo = audioServices.GetAudioServicesRepo();
            audioServicesRepo.TriggeredRebuild += AudioServicesRepo_TriggeredRebuild;

            ILibraryRepo libraryRepo = audioServices.GetLibraryRepo();
            libraryRepo.PlayStateChanged += LibraryRepo_PlayStateChanged;

            foreach (ICommunicator communicator in audioServices.GetCommunicators())
            {
                communicator.Disconnected += Communicator_Disconnected;
            }
        }

        private void Unsubscribe(AudioServices audioServices)
        {
            if (audioServices == null) return;

            IAudioServicesRepo audioServicesRepo = audioServices.GetAudioServicesRepo();
            audioServicesRepo.TriggeredRebuild -= AudioServicesRepo_TriggeredRebuild;

            ILibraryRepo libraryRepo = audioServices.GetLibraryRepo();
            libraryRepo.PlayStateChanged -= LibraryRepo_PlayStateChanged;

            foreach (ICommunicator communicator in audioServices.GetCommunicators())
            {
                communicator.Disconnected -= Communicator_Disconnected;
            }
        }

        private void AudioServicesRepo_TriggeredRebuild(object sender, AudioServicesTriggeredRebuildArgs e)
        {
            if (e.Source == AudioServicesRebuildSource.Foreground)
            {
                runSem.Release();
            }
        }

        private void LibraryRepo_PlayStateChanged(object sender, AudioLibraryChangeArgs<PlaybackState> e)
        {
            playState = e.NewValue;
            if (e.NewValue == PlaybackState.Paused) closeTimer.Reset();
        }

        private void CloseTimer_RanDown(object sender, EventArgs e)
        {
            if (playState == PlaybackState.Paused) TriggerStop();
        }

        private void Communicator_Disconnected(object sender, DisconnectedEventArgs e)
        {
            runSem.Release();
        }

        private void TriggerStop()
        {
            stop = true;
            runSem.Release();
        }

        private void Instance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            TriggerStop();
        }
    }
}
