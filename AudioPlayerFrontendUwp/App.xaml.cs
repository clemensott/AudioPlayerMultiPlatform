using AudioPlayerFrontend.Join;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using AudioPlayerBackend.Build;
using StdOttUwp.BackPress;
using System.Threading.Tasks;
using AudioPlayerFrontend.Background;
using Windows.ApplicationModel.Background;
using StdOttStandard.Dispatch;
using AudioPlayerBackend;
using AudioPlayerFrontend.Extensions;
using AudioPlayerBackend.FileSystem;
using AudioPlayerBackend.Player;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using System.Net;
using AudioPlayerBackendUwpLib;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    sealed partial class App : Application
    {
        private const string serviceProfileFilename = "serviceProfile.data";
        private readonly TimeSpan autoUpdateInverval = TimeSpan.FromDays(1),
            autoUpdatePlaylistsInterval = TimeSpan.FromHours(1);

        private readonly AudioServicesHandler audioServicesHandler;
        private readonly AudioServicesBuilderNavigationHandler audioServicesBuilderNavigationHandler;
        private readonly BackgroundTaskHandler backgroundTaskHandler;
        private readonly BackgroundTaskHelper backgroundTaskHelper;
        private Task loadServiceProfileTask = null;

        public App()
        {
            Logs.SetFileSystemService(new FileSystemService());
            Logs.Log("App1");
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            //this.UnhandledException += OnUnhandledException; // TODO: revert
            this.LeavingBackground += OnLeavingBackground;

            loadServiceProfileTask = Task.Run(StartAudioServicesHandler);

            Dispatcher dispatcher = new Dispatcher();
            audioServicesHandler = new AudioServicesHandler(dispatcher, AudioServicesBuildConfigTransformer);
            audioServicesHandler.ServicesBuild += OnServicesBuild;
            audioServicesBuilderNavigationHandler = new AudioServicesBuilderNavigationHandler(audioServicesHandler);
            backgroundTaskHandler = new BackgroundTaskHandler(dispatcher, audioServicesHandler);
            backgroundTaskHelper = new BackgroundTaskHelper(audioServicesHandler);
        }

        //private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    Settings.Current.SetUnhandledException(e.Exception);
        //}

        private static AudioServicesBuildConfig AudioServicesBuildConfigTransformer(AudioServicesBuildConfig originalConfig)
        {
            if (originalConfig.BuildClient) return originalConfig;

            AudioServicesBuildConfig transformedConfig = originalConfig.Clone();
            if (originalConfig.BuildStandalone)
            {
                transformedConfig.WithClient(IPAddress.Loopback.ToString(), Settings.Current.BackgroundTaskPort);
            }
            else if (originalConfig.BuildServer)
            {
                transformedConfig.WithClient(IPAddress.Loopback.ToString(), originalConfig.ServerPort);
            }

            return transformedConfig;

        }

        /// <summary>
        /// Wird aufgerufen, wenn die Anwendung durch den Endbenutzer normal gestartet wird. Weitere Einstiegspunkte
        /// werden z. B. verwendet, wenn die Anwendung gestartet wird, um eine bestimmte Datei zu öffnen.
        /// </summary>
        /// <param name="e">Details über Startanforderung und -prozess.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            BackPressHandler.Current.Activate();

            Frame rootFrame = Window.Current.Content as Frame;

            // App-Initialisierung nicht wiederholen, wenn das Fenster bereits Inhalte enthält.
            // Nur sicherstellen, dass das Fenster aktiv ist.
            if (rootFrame == null) Window.Current.Content = rootFrame = new Frame();
            if (rootFrame.Content == null)
            {
                await Task.Yield(); // let OnLeavingBackground fire to start background task asap

                audioServicesBuilderNavigationHandler.Start(rootFrame);

                await loadServiceProfileTask;
                loadServiceProfileTask = null; // release memory
            }

            // Sicherstellen, dass das aktuelle Fenster aktiv ist
            Window.Current.Activate();
        }

        private async Task StartAudioServicesHandler()
        {
            //var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync("library.db");
            //if (item is StorageFile file) await file.DeleteAsync();

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

            audioServicesHandler.Start(config);
        }

        private async void OnServicesBuild(object sender, AudioServicesBuilder e)
        {
            // save service profile if service was built successfully by it
            if (audioServicesHandler.Config != null)
            {
                ServiceProfile profile = audioServicesHandler.Config.ToServiceProfile();
                await profile.Save();
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Navigation auf eine bestimmte Seite fehlschlägt
        /// </summary>
        /// <param name="sender">Der Rahmen, bei dem die Navigation fehlgeschlagen ist</param>
        /// <param name="e">Details über den Navigationsfehler</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Ausführung der Anwendung angehalten wird.  Der Anwendungszustand wird gespeichert,
        /// ohne zu wissen, ob die Anwendung beendet oder fortgesetzt wird und die Speicherinhalte dabei
        /// unbeschädigt bleiben.
        /// </summary>
        /// <param name="sender">Die Quelle der Anhalteanforderung.</param>
        /// <param name="e">Details zur Anhalteanforderung.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            Settings.Current.SuspendTime = DateTime.Now;
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            try
            {
                backgroundTaskHandler.Stop();

                await audioServicesHandler.Stop();
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine("Saving service profile failed:\n" + exc);
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            if (!backgroundTaskHandler.IsRunning) await backgroundTaskHelper.Start();
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            BackgroundTaskDeferral deferral = args.TaskInstance.GetDeferral();
            await backgroundTaskHandler.Run();
            deferral.Complete();
        }
    }
}
