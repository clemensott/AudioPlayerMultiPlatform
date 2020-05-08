using AudioPlayerFrontend.Join;
using System;
using System.IO;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using AudioPlayerBackend.Build;
using StdOttUwp;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    sealed partial class App : Application
    {
        private const string serviceProfileFilename = "serviceProfile.xml";

        public static DateTime CreateTime = DateTime.MinValue;
        public static StorageFile ExceptionFile;
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(ServiceProfile));

        private readonly ServiceBuilder serviceBuilder;
        private readonly ViewModel viewModel;

        public App()
        {
            CreateTime = DateTime.Now;

            this.InitializeComponent();
            this.Suspending += OnSuspending;

            serviceBuilder = new ServiceBuilder(ServiceBuilderHelper.Current);
            serviceBuilder.WithPlayer(new Player());

            viewModel = new ViewModel(serviceBuilder);

            UnhandledException += App_UnhandledException;

            EnteredBackground += Application_EnteredBackground;
            LeavingBackground += Application_LeavingBackground;
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            FileIO.WriteTextAsync(ExceptionFile, e.Exception.ToString()).AsTask().Wait();
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Anwendung durch den Endbenutzer normal gestartet wird. Weitere Einstiegspunkte
        /// werden z. B. verwendet, wenn die Anwendung gestartet wird, um eine bestimmte Datei zu öffnen.
        /// </summary>
        /// <param name="e">Details über Startanforderung und -prozess.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            BackPressHandler.Current.Activate();
            LoadExceptionFile();

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

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    try
                    {
                        StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(serviceProfileFilename);
                        string xmlText = await FileIO.ReadTextAsync(file);
                        StringReader reader = new StringReader(xmlText);

                        ServiceProfile profile = (ServiceProfile)serializer.Deserialize(reader);
                        profile.ToClient();
                        profile.FillServiceBuilderWithMinimum(serviceBuilder);
                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Debug.WriteLine("Loading service profile failed:\n" + exc);

                        serviceBuilder.WithClient("nas-server", 1884);
                    }

                    rootFrame.Navigate(typeof(MainPage), viewModel);
                }
                // Sicherstellen, dass das aktuelle Fenster aktiv ist
                Window.Current.Activate();
            }
        }

        private async void LoadExceptionFile()
        {
            ExceptionFile = await ApplicationData.Current.LocalFolder
                .CreateFileAsync("Exception.txt", CreationCollisionOption.OpenIfExists);
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
            System.Diagnostics.Debug.WriteLine("Suspend: " + viewModel.Communicator?.IsOpen);
            var deferral = e.SuspendingOperation.GetDeferral();

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

        private async void Application_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("EnterBackground: " + viewModel.Communicator?.IsOpen);

            viewModel.ServiceOpenBuild?.Cancel();

            if (viewModel.Communicator?.IsOpen != true) return;

            Deferral deferral = e.GetDeferral();

            try
            {
                await viewModel.CloseAsync();
            }
            catch { }

            deferral.Complete();
        }

        private static DateTime lastConnectStart;
        private async void Application_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            Frame frame = viewModel.Frame;

            System.Diagnostics.Debug.WriteLine("LeaveBackground1: " + GetDebugText(frame));

            if (frame == null) return;

            try
            {
                if (frame.CurrentSourcePageType == typeof(BuildOpenPage))
                {
                    frame.GoBack();

                    System.Diagnostics.Debug.WriteLine("GoBack: " + GetDebugText(frame));
                    await new Windows.UI.Popups.MessageDialog(GetDebugText(frame), "App_LeaveBackground").ShowAsync();
                }
            }
            catch (Exception exc)
            {
                await new Windows.UI.Popups.MessageDialog(exc.ToString(), "App_LeaveBackgroundFail").ShowAsync();
                return;
            }

            lastConnectStart = DateTime.Now;
            await viewModel.ConnectAsync();
            System.Diagnostics.Debug.WriteLine("LeaveBackground2: " + (viewModel.Communicator != null));
        }

        private string GetDebugText(Frame frame)
        {
            string pageType = frame?.CurrentSourcePageType != null ? frame.CurrentSourcePageType.ToString() : "Page==null";
            string communicator = viewModel.Communicator?.ToString() ?? "Communicator==null";
            string build = viewModel.ServiceOpenBuild?.ToString() ?? "Build==null";
            string buildState = viewModel.ServiceOpenBuild?.State.ToString() ?? "BuildState==null";
            string comState = viewModel.ServiceOpenBuild?.CommunicatorToken.IsEnded?.ToString() ?? "CommunicatorState==null";
            string syncState = viewModel.ServiceOpenBuild?.SyncToken.IsEnded?.ToString() ?? "SyncState==null";
            string playerState = viewModel.ServiceOpenBuild?.PlayerToken.IsEnded?.ToString() ?? "PlayerState==null";
            string copState = viewModel.ServiceOpenBuild?.CompleteToken.IsEnded?.ToString() ?? "CompleteState==null";

            return string.Join("\r\n", pageType, communicator, build, buildState, comState, syncState, playerState, copState, lastConnectStart);
        }
    }
}
