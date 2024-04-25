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
using System.ComponentModel;
using AudioPlayerBackend.Communication;
using Windows.Foundation;
using AudioPlayerBackend;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    sealed partial class App : Application
    {
        private const string serviceProfileFilename = "serviceProfile.xml";
        private readonly TimeSpan autoUpdateInverval = TimeSpan.FromDays(1),
            autoUpdatePlaylistsInterval = TimeSpan.FromHours(1);

        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(ServiceProfile));

        private readonly ViewModel viewModel;
        private readonly TaskCompletionSource<bool> launchCompletionSource;
        private readonly BackgroundTaskHandler backgroundTaskHandler;
        private readonly BackgroundTaskHelper backgroundTaskHelper;
        private Frame rootFrame;
        private bool canceledBuild;
        private DateTime lastAutoUpdatePlaylists;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += OnUnhandledException;
            this.EnteredBackground += OnEnteredBackground;
            this.LeavingBackground += OnLeavingBackground;

            AudioPlayerServiceProvider.Current
                .AddFileSystemService<FileSystemService>()
                .AddDispatcher<InvokeDispatcherService>()
                .AddPlayerCreateService<PlayerCreateService>()
                .Build();

            Dispatcher dispatcher = new Dispatcher();
            ServiceBuilder serviceBuilder = new ServiceBuilder();
            ServiceHandler service = new ServiceHandler(dispatcher, serviceBuilder);
            service.PropertyChanged += Service_PropertyChanged;

            viewModel = new ViewModel(service);
            launchCompletionSource = new TaskCompletionSource<bool>();
            backgroundTaskHandler = new BackgroundTaskHandler(dispatcher, service);
            backgroundTaskHelper = new BackgroundTaskHelper();
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
            rootFrame = Window.Current.Content as Frame;

            BackPressHandler.Current.Activate();

            Task loadServiceProfileTask = Task.CompletedTask;

            // App-Initialisierung nicht wiederholen, wenn das Fenster bereits Inhalte enthält.
            // Nur sicherstellen, dass das Fenster aktiv ist.
            if (rootFrame == null) Window.Current.Content = rootFrame = new Frame();
            if (rootFrame.Content == null)
            {
                loadServiceProfileTask = LoadServiceProfile();
                rootFrame.Navigate(typeof(MainPage), viewModel);
            }
            // Sicherstellen, dass das aktuelle Fenster aktiv ist
            Window.Current.Activate();

            await loadServiceProfileTask;
            launchCompletionSource.TrySetResult(true);
        }

        private async Task LoadServiceProfile()
        {
            try
            {
                IStorageItem item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(serviceProfileFilename);
                if (item is StorageFile)
                {
                    string xmlText = await FileIO.ReadTextAsync((StorageFile)item);
                    StringReader reader = new StringReader(xmlText);

                    ServiceProfile profile = (ServiceProfile)serializer.Deserialize(reader);
                    profile.FillServiceBuilder(viewModel.Service.Builder);
                }
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

                ServiceProfile profile = new ServiceProfile(viewModel.Service.Builder);
                StringWriter writer = new StringWriter();
                serializer.Serialize(writer, profile);

                StorageFile file = await ApplicationData.Current.LocalFolder
                    .CreateFileAsync(serviceProfileFilename, CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file, writer.ToString());

                viewModel.Service.Communicator?.Dispose();
                viewModel.Service.ServicePlayer?.Dispose();
                viewModel.Service.Data?.Dispose();
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

        private void Service_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ServiceHandler service = (ServiceHandler)sender;
            ServiceBuild build = service.ServiceOpenBuild;

            if (e.PropertyName == nameof(ServiceHandler.ServiceOpenBuild) && build != null)
            {
                rootFrame.Navigate(typeof(BuildOpenPage), build);
            }
        }

        private async void OnEnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            ServiceBuild build = viewModel.Service.ServiceOpenBuild;
            if (build?.CompleteToken.IsEnded.HasValue != false) return;

            Deferral deferral = e.GetDeferral();

            try
            {
                await viewModel.Service.CloseAsync();
                canceledBuild = true;
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            if (!backgroundTaskHandler.IsRunning) await backgroundTaskHelper.Start();

            await launchCompletionSource.Task;

            ServiceBuildResult result;
            if (canceledBuild && viewModel.Service.ServiceOpenBuild == null)
            {
                result = await viewModel.Service.ConnectAsync(true);
            }
            else if (viewModel.Service.Audio == null || viewModel.Service.Communicator?.IsOpen == false)
            {
                result = await viewModel.Service.ConnectAsync(false);
            }
            else return;

            if (result == null || result.Communicator is IClientCommunicator || viewModel.IsUpdatingPlaylists) return;

            try
            {
                viewModel.IsUpdatingPlaylists = true;

                if (Settings.Current.LastUpdatedData > lastAutoUpdatePlaylists)
                {
                    lastAutoUpdatePlaylists = Settings.Current.LastUpdatedData;
                }

                if (DateTime.Now - Settings.Current.LastUpdatedData > autoUpdateInverval)
                {
                    await UpdateHelper.Update(result.AudioService);
                    Settings.Current.LastUpdatedData = DateTime.Now;
                }
                else if (DateTime.Now - lastAutoUpdatePlaylists > autoUpdatePlaylistsInterval)
                {
                    await UpdateHelper.UpdatePlaylists(result.AudioService);
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
            BackgroundTaskDeferral deferral = args.TaskInstance.GetDeferral();
            await backgroundTaskHandler.Run();
            deferral.Complete();
        }
    }
}
