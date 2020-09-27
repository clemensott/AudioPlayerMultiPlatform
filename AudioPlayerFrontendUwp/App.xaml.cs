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
using StdOttUwp;
using System.Threading.Tasks;
using AudioPlayerFrontend.Background;
using Windows.ApplicationModel.Background;
using StdOttStandard.Dispatch;
using System.ComponentModel;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    sealed partial class App : Application
    {
        private const string serviceProfileFilename = "serviceProfile.xml";

        public static DateTime CreateTime = DateTime.MinValue;
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(ServiceProfile));

        private readonly ServiceBuilder serviceBuilder;
        private readonly ViewModel viewModel;
        private Frame rootFrame;

        public App()
        {
            CreateTime = DateTime.Now;

            this.InitializeComponent();
            this.Suspending += OnSuspending;

            serviceBuilder = new ServiceBuilder(ServiceBuilderHelper.Current);
            serviceBuilder.WithNotifyPropertyChangedHelper(ServiceBuilderHelper.Current);

            Dispatcher dispatcher = new Dispatcher();
            ServiceHandler service = new ServiceHandler(dispatcher, serviceBuilder);
            service.PropertyChanged += Service_PropertyChanged;

            viewModel = new ViewModel(service);
            BackgroundTaskHandler.Current = new BackgroundTaskHandler(dispatcher, service);

            UnhandledException += App_UnhandledException;
            LeavingBackground += Application_LeavingBackground;
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
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

            // App-Initialisierung nicht wiederholen, wenn das Fenster bereits Inhalte enthält.
            // Nur sicherstellen, dass das Fenster aktiv ist.
            if (rootFrame == null)
            {
                // Frame erstellen, der als Navigationskontext fungiert und zum Parameter der ersten Seite navigieren
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Zustand von zuvor angehaltener Anwendung laden
                }

                // Den Frame im aktuellen Fenster platzieren
                Window.Current.Content = rootFrame;
            }

            Task buildTask = Task.CompletedTask;

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    try
                    {
                        StorageFile file =
                            await ApplicationData.Current.LocalFolder.GetFileAsync(serviceProfileFilename);
                        string xmlText = await FileIO.ReadTextAsync(file);
                        StringReader reader = new StringReader(xmlText);

                        ServiceProfile profile = (ServiceProfile)serializer.Deserialize(reader);
                        profile.ToClient();
                        profile.FillServiceBuilderWithMinimum(serviceBuilder);
                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Debug.WriteLine("Loading service profile failed:\n" + exc);
                    }

                    rootFrame.Navigate(typeof(MainPage), viewModel);
                    buildTask = viewModel.Service.ConnectAsync();
                }
                // Sicherstellen, dass das aktuelle Fenster aktiv ist
                Window.Current.Activate();
            }

            await buildTask;
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
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            BackgroundTaskHandler.Current.Stop();

            try
            {
                ServiceProfile profile = new ServiceProfile(serviceBuilder);
                StringWriter writer = new StringWriter();
                serializer.Serialize(writer, profile);

                StorageFile file = await ApplicationData.Current.LocalFolder
                    .CreateFileAsync(serviceProfileFilename, CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file, writer.ToString());
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine("Saving service profile failed:\n" + exc);
            }

            deferral.Complete();
        }

        private void Service_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ServiceHandler service = (ServiceHandler)sender;
            if (e.PropertyName == nameof(ServiceHandler.ServiceOpenBuild) && service.ServiceOpenBuild != null)
            {
                rootFrame.Navigate(typeof(BuildOpenPage), service.ServiceOpenBuild);
            }
        }

        private async void Application_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            if (!BackgroundTaskHandler.Current.IsRunning) await BackgroundTaskHelper.Current.Start();
            if (viewModel.Service.Communicator?.IsOpen == false) await viewModel.Service.ConnectAsync();
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            BackgroundTaskDeferral deferral = args.TaskInstance.GetDeferral();
            await BackgroundTaskHandler.Current.Run();
            deferral.Complete();
        }
    }
}
