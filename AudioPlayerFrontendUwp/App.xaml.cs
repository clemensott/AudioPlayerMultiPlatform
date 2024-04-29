using AudioPlayerFrontend.Join;
using System;
using System.IO;
using System.Xml.Serialization;
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
using System.ComponentModel;
using Newtonsoft.Json;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    sealed partial class App : Application
    {
        private const string serviceProfileFilename = "serviceProfile.json";
        private readonly TimeSpan autoUpdateInverval = TimeSpan.FromDays(1),
            autoUpdatePlaylistsInterval = TimeSpan.FromHours(1);

        private readonly ServiceHandler serviceHandler;
        private readonly BackgroundTaskHandler backgroundTaskHandler;
        private readonly BackgroundTaskHelper backgroundTaskHelper;
        private DateTime lastAutoUpdatePlaylists;
        Task loadServiceProfileTask = null;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += OnUnhandledException;
            this.LeavingBackground += OnLeavingBackground;

            Logs.Log("App1");
            loadServiceProfileTask = Task.Run(LoadServiceProfile);
            AudioPlayerServiceProvider.Current
                .AddFileSystemService<FileSystemService>()
                .AddDispatcher<InvokeDispatcherService>()
                .AddPlayerCreateService<PlayerCreateService>()
                .Build();
            Logs.Log("App2");

            Dispatcher dispatcher = new Dispatcher();
            ViewModel viewModel = new ViewModel();
            Logs.Log("App3");
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            serviceHandler = new ServiceHandler(dispatcher, viewModel)
            {
                Builder = new ServiceBuilder(),
            };

            Logs.Log("App4");
            backgroundTaskHandler = new BackgroundTaskHandler(dispatcher, serviceHandler);
            backgroundTaskHelper = new BackgroundTaskHelper();
            Logs.Log("App5");
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

            Frame rootFrame = Window.Current.Content as Frame;

            // App-Initialisierung nicht wiederholen, wenn das Fenster bereits Inhalte enthält.
            // Nur sicherstellen, dass das Fenster aktiv ist.
            if (rootFrame == null) Window.Current.Content = rootFrame = new Frame();
            Logs.Log("OnLaunched3");
            if (rootFrame.Content == null)
            {
                await Task.Yield(); // let OnLeavingBackground fire to start background task asap
                Logs.Log("OnLaunched4");
                rootFrame.NavigateToBuildOpenPage(serviceHandler);
                Logs.Log("OnLaunched5");
                await loadServiceProfileTask;
                Logs.Log("OnLaunched6");
                await serviceHandler.Start(rootFrame);
            }

            // Sicherstellen, dass das aktuelle Fenster aktiv ist
            Window.Current.Activate();

            Logs.Log("OnLaunched8");
        }

        private async Task LoadServiceProfile()
        {
            try
            {
                Logs.Log("LoadServiceProfile1");
                IStorageItem item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(serviceProfileFilename);
                Logs.Log("LoadServiceProfile2");
                if (item is StorageFile)
                {
                    string jsonText = await FileIO.ReadTextAsync((StorageFile)item);
                    Logs.Log("LoadServiceProfile3", jsonText.Length);
                    ServiceProfile profile = JsonConvert.DeserializeObject<ServiceProfile>(jsonText);
                    Logs.Log("LoadServiceProfile4");
                    profile.FillServiceBuilder(serviceHandler.Builder);
                }
                Logs.Log("LoadServiceProfile5");
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine("Loading service profile failed:\n" + exc);
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

                ServiceProfile profile = new ServiceProfile(serviceHandler.Builder);
                string jsonText = JsonConvert.SerializeObject(profile);

                StorageFile file = await ApplicationData.Current.LocalFolder
                    .CreateFileAsync(serviceProfileFilename, CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file, jsonText);

                await serviceHandler.CloseAsync();
                serviceHandler.Dispose();
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
            Logs.Log("OnLeavingBackground1", backgroundTaskHandler.IsRunning);
            if (!backgroundTaskHandler.IsRunning) await backgroundTaskHelper.Start();
        }

        private async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(serviceHandler.ViewModel.Audio))
            {
                await Task.Yield(); // Resume building/setting of audio service asap
                await AutoUpdate();
            }
        }

        private async Task AutoUpdate()
        {
            ViewModel viewModel = serviceHandler.ViewModel;
            if (viewModel.Audio == null
                || viewModel.IsClient
                || viewModel.IsUpdatingPlaylists) return;

            try
            {
                viewModel.IsUpdatingPlaylists = true;

                if (Settings.Current.LastUpdatedData > lastAutoUpdatePlaylists)
                {
                    lastAutoUpdatePlaylists = Settings.Current.LastUpdatedData;
                }

                if (DateTime.Now - Settings.Current.LastUpdatedData > autoUpdateInverval)
                {
                    await UpdateHelper.Update(viewModel.Audio);
                    Settings.Current.LastUpdatedData = DateTime.Now;
                }
                else if (DateTime.Now - lastAutoUpdatePlaylists > autoUpdatePlaylistsInterval)
                {
                    await UpdateHelper.UpdatePlaylists(viewModel.Audio);
                    lastAutoUpdatePlaylists = DateTime.Now;
                }
            }
            catch { }
            finally
            {
                viewModel.IsUpdatingPlaylists = false;
            }
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            Logs.Log("OnBackgroundActivated1");
            BackgroundTaskDeferral deferral = args.TaskInstance.GetDeferral();
            await backgroundTaskHandler.Run();
            deferral.Complete();
        }
    }
}
