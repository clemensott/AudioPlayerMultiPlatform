using AudioPlayerBackend;
using AudioPlayerFrontend.Join;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    sealed partial class App : Application
    {
        public static DateTime CreateTime = DateTime.MinValue;
        private const string serviceProfileFilename = "serviceProfile.xml";
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(ServiceProfile));
        /// <summary>
        /// Initialisiert das Singletonanwendungsobjekt. Dies ist die erste Zeile von erstelltem Code
        /// und daher das logische Äquivalent von main() bzw. WinMain().
        /// </summary>

        private ServiceBuilder serviceBuilder;
        private ViewModel viewModel;
        private StorageFile exceptionFile;

        public App()
        {
            CreateTime = DateTime.Now;

            this.InitializeComponent();
            this.Suspending += OnSuspending;

            serviceBuilder = new ServiceBuilder(ServiceBuilderHelper.Current);
            serviceBuilder.WithPlayer(new Join.Player());

            viewModel = new ViewModel(serviceBuilder);

            UnhandledException += App_UnhandledException;

            EnteredBackground += Application_EnteredBackground;
            LeavingBackground += Application_LeavingBackground;
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            FileIO.WriteTextAsync(exceptionFile, e.Exception.ToString()).AsTask().Wait();
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Anwendung durch den Endbenutzer normal gestartet wird. Weitere Einstiegspunkte
        /// werden z. B. verwendet, wenn die Anwendung gestartet wird, um eine bestimmte Datei zu öffnen.
        /// </summary>
        /// <param name="e">Details über Startanforderung und -prozess.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            exceptionFile = await ApplicationData.Current.LocalFolder
                .CreateFileAsync("Exception.txt", CreationCollisionOption.OpenIfExists);

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
                        //serviceBuilder.WithClient("nas-server", 1884);
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

                if (!viewModel.IsTryOpening && viewModel.AudioService == null) await viewModel.BuildAsync();
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
            if (viewModel.Communicator?.IsOpen == true)
            {
                var deferral = e.GetDeferral();

                try
                {
                    //if (mqttAudio is IMqttAudioClient mqtttAudioClient)
                    //{
                    //    mqtttAudioClient.MqttClient.Disconnected -= MqttClient_Disconnected;
                    //}

                    await viewModel.Communicator.CloseAsync();
                }
                catch (Exception exc)
                {
                    //await new MessageDialog(exc.ToString(), "EnteredBackground").ShowAsync();

                    //builder.WithService(mqttAudio);

                    //Frame.Navigate(typeof(SettingsPage), builder);
                }

                deferral.Complete();
            }

        }

        private async void Application_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("LeaveBackground1: " + viewModel.Communicator?.IsOpen);
            if (viewModel.Communicator != null) await viewModel.OpenAsync(viewModel.Communicator);
            System.Diagnostics.Debug.WriteLine("LeaveBackground2: " + viewModel.Communicator?.IsOpen);
        }
    }
}
