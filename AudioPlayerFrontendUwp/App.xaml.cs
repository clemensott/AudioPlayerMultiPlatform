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
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

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

        private readonly MemoryHandler memoryHandler;
        private Task loadServiceProfileTask = null;

        internal AudioServicesHandler AudioServicesHandler { get; }

        internal ForegroundTaskHandler ForegroundTaskHandler { get; }

        internal BackgroundTaskHandler BackgroundTaskHandler { get; }

        internal BackgroundTaskHelper BackgroundTaskHelper { get; }

        public App()
        {
            Logs.SetFileSystemService(new FileSystemService());
            Logs.Log("App1");
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += OnUnhandledException;

            loadServiceProfileTask = Task.Run(StartAudioServicesHandler);

            Dispatcher dispatcher = new Dispatcher();
            AudioServicesHandler = new AudioServicesHandler(dispatcher);
            AudioServicesHandler.AudioServicesChanged += OnAudioServicesChanged;

            BackgroundTaskHelper = new BackgroundTaskHelper();
            ForegroundTaskHandler = new ForegroundTaskHandler(AudioServicesHandler);
            BackgroundTaskHandler = new BackgroundTaskHandler(dispatcher, AudioServicesHandler);
            memoryHandler = new MemoryHandler(this);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Settings.Current.SetUnhandledException(e.Exception);
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Anwendung durch den Endbenutzer normal gestartet wird. Weitere Einstiegspunkte
        /// werden z. B. verwendet, wenn die Anwendung gestartet wird, um eine bestimmte Datei zu öffnen.
        /// </summary>
        /// <param name="e">Details über Startanforderung und -prozess.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            BackPressHandler.Current.Activate();

            memoryHandler.Start();

            await (loadServiceProfileTask ?? Task.CompletedTask);
            loadServiceProfileTask = null; // release memory

            ForegroundTaskHandler.Start();
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

            ServiceProfile? profile = await LoadServiceProfile();
            if (profile.HasValue) config.WithServiceProfile(profile.Value);

            AudioServicesHandler.Start(config);
        }

        private async Task<ServiceProfile?> LoadServiceProfile()
        {
            try
            {
                IStorageItem item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(serviceProfileFilename);
                if (item is StorageFile)
                {
                    IBuffer buffer = await FileIO.ReadBufferAsync((StorageFile)item);
                    ServiceProfile profile = ServiceProfile.FromData(buffer.ToArray());
                    return profile;
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine("Loading service profile failed:\n" + exc);
            }

            return null;
        }

        private async void OnAudioServicesChanged(object sender, AudioServicesChangedEventArgs e)
        {
            // save service profile if service was built successfully by it
            if (AudioServicesHandler.Config != null)
            {
                ServiceProfile profile = AudioServicesHandler.Config.ToServiceProfile();
                byte[] data = profile.ToData();

                StorageFile file = await ApplicationData.Current.LocalFolder
                    .CreateFileAsync(serviceProfileFilename, CreationCollisionOption.OpenIfExists);
                await FileIO.WriteBytesAsync(file, data);
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
                Logs.Log("App.Suspending");
                BackgroundTaskHandler.Stop();

                await AudioServicesHandler.Stop();
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

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            BackgroundTaskDeferral deferral = args.TaskInstance.GetDeferral();
            await BackgroundTaskHandler.Run();
            deferral.Complete();
        }
    }
}
