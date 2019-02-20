using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static readonly TimeSpan networkConnectionTimeOut = TimeSpan.FromMilliseconds(200),
            networkConnectionMaxTime = TimeSpan.FromSeconds(5);

        private ServiceBuilder builder;
        private Task<IAudioExtended> buildTask;
        private ViewModel viewModel;

        public MainPage()
        {
            this.InitializeComponent();

            DataContext = viewModel = new ViewModel();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            tbxSearchKey.IsEnabled = false;

            if (e.Parameter is ServiceBuilder)
            {
                builder = e.Parameter as ServiceBuilder;

                if (builder.Player == null) builder.WithPlayer(new Join.Player());

                buildTask = builder.Build();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.EnteredBackground += Application_EnteredBackground;
            Application.Current.LeavingBackground += Application_LeavingBackground;

            if (await BuildAsync(builder, buildTask))
            {
                if (viewModel.AudioService is IMqttAudioClient mqttAudioClient) mqttAudioClient.MqttClient.Disconnected += MqttClient_Disconnected;

                tbxSearchKey.IsEnabled = true;
            }
            else Frame.Navigate(typeof(SettingsPage), builder);
        }

        private async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Application.Current.EnteredBackground -= Application_EnteredBackground;
            Application.Current.LeavingBackground -= Application_LeavingBackground;

            try
            {
                if (viewModel.AudioService?.Base is IMqttAudio mqttAudio)
                {
                    if (mqttAudio is IMqttAudioClient mqtttAudioClient) mqtttAudioClient.MqttClient.Disconnected -= MqttClient_Disconnected;

                    await mqttAudio.CloseAsync();
                }
            }
            catch { }
        }

        private async void Application_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            IMqttAudio mqttAudio = viewModel.AudioService?.Base as IMqttAudio;

            if (await OpenAsync(mqttAudio) && mqttAudio is IMqttAudioClient mqtttAudioClient)
            {
                mqtttAudioClient.MqttClient.Disconnected += MqttClient_Disconnected;
            }
        }

        private async void Application_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            if (viewModel.AudioService?.Base is IMqttAudio mqttAudio && mqttAudio.IsOpen)
            {
                try
                {
                    if (mqttAudio is IMqttAudioClient mqtttAudioClient) mqtttAudioClient.MqttClient.Disconnected -= MqttClient_Disconnected;

                    await mqttAudio.CloseAsync();
                }
                catch (Exception exc)
                {
                    await new MessageDialog(exc.ToString(), "EnteredBackground").ShowAsync();

                    builder.WithService(mqttAudio);

                    Frame.Navigate(typeof(SettingsPage), builder);
                }
            }
        }

        private async void MqttClient_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                string message = string.Format("Was connected: {0}\r\nException: {1}", e.ClientWasConnected, e.Exception?.ToString() ?? "null");
                await new MessageDialog(message, "MqttClient_Disconnected").ShowAsync();

                IMqttAudioClient mqtttAudioClient = viewModel.AudioService?.Base as IMqttAudioClient;
                mqtttAudioClient.MqttClient.Disconnected -= MqttClient_Disconnected;

                if (await OpenAsync(mqtttAudioClient)) mqtttAudioClient.MqttClient.Disconnected += MqttClient_Disconnected;
            });
        }

        private async Task<bool> BuildAsync(ServiceBuilder builder, Task<IAudioExtended> buildTask)
        {
            viewModel.IsTryOpening = true;

            while (true)
            {
                try
                {
                    viewModel.AudioService = new AudioViewModel(await buildTask);
                    break;
                }
                catch
                {
                    await Task.Delay(200);

                    if (viewModel.IsTryOpening)
                    {
                        buildTask = builder.Build();
                        continue;
                    }

                    viewModel.AudioService = null;
                    break;
                }
            }

            viewModel.IsTryOpening = false;

            return viewModel.AudioService != null;
        }

        private async Task<bool> OpenAsync(IMqttAudio mqttAudio)
        {
            if (mqttAudio == null || mqttAudio.IsOpen) return true;

            viewModel.IsTryOpening = true;

            while (true)
            {
                try
                {
                    await mqttAudio.OpenAsync();
                    break;
                }
                catch (Exception exc)
                {
                    await Task.Delay(500);

                    if (viewModel.IsTryOpening) continue;

                    await new MessageDialog(exc.ToString(), "LeavingBackground").ShowAsync();

                    builder.WithService(mqttAudio);
                    Frame.Navigate(typeof(SettingsPage), builder);

                    break;
                }
            }

            viewModel.IsTryOpening = false;

            return mqttAudio.IsOpen;
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.Reload();
        }

        private void TbxSearchKey_GotFocus(object sender, RoutedEventArgs e)
        {
            if (viewModel != null) viewModel.ViewAdvancedSettings = false;
        }

        private void LbxSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Scroll();
        }

        private void Scroll()
        {
            if (lbxSongs.SelectedItem != null) lbxSongs.ScrollIntoView(lbxSongs.SelectedItem);
            else if (lbxSongs.Items.Count > 0) lbxSongs.ScrollIntoView(lbxSongs.Items[0]);
        }

        private void AbbPrevious_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.SetPreviousSong();
        }

        private void AbbPlayPause_Click(object sender, RoutedEventArgs e)
        {
            AudioViewModel audio = viewModel.AudioService;

            if (audio == null) return;

            audio.PlayState = audio.PlayState == PlaybackState.Playing ? PlaybackState.Paused : PlaybackState.Playing;
        }

        private void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.SetNextSong();
        }

        private void AbbSettings_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.AudioService != null) builder.WithService(viewModel.AudioService.Base);

            Frame.Navigate(typeof(SettingsPage), builder);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            viewModel.IsTryOpening = false;
        }

        private async void AbbDebug_Click(object sender, RoutedEventArgs e)
        {
            await new MessageDialog("CreateTime: " + App.CreateTime).ShowAsync();
            //await new MessageDialog("Type: " + (viewModel.AudioService?.Base.GetType().ToString() ?? "null")).ShowAsync();
            await new MessageDialog("IsOpen: " + ((viewModel.AudioService?.Base as IMqttAudio)?.IsOpen.ToString() ?? "null")).ShowAsync();

            string exceptionText;

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("Exception.txt");

                exceptionText = await FileIO.ReadTextAsync(file);
            }
            catch (Exception exc)
            {
                exceptionText = exc.ToString();
            }

            await new MessageDialog(exceptionText).ShowAsync();
        }
    }
}
