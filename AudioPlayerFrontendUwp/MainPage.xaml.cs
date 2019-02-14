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

                    if (viewModel.IsTryOpening) continue;

                    DataContext = viewModel = null;
                    break;
                }
            }

            viewModel.IsTryOpening = false;

            if (viewModel == null) Frame.Navigate(typeof(SettingsPage), builder);
            else tbxSearchKey.IsEnabled = true;
        }

        private async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Application.Current.EnteredBackground -= Application_EnteredBackground;
            Application.Current.LeavingBackground -= Application_LeavingBackground;

            try
            {
                if (viewModel.AudioService != null && viewModel.AudioService.Base is IMqttAudio mqttService) await mqttService.CloseAsync();
            }
            catch { }
        }

        private async void Application_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            IMqttAudio audio = viewModel.AudioService?.Base as IMqttAudio;

            if (audio == null || audio.IsOpen) return;

            viewModel.IsTryOpening = true;

            while (true)
            {
                try
                {
                    await audio.OpenAsync();

                    break;
                }
                catch (Exception exc)
                {
                    await Task.Delay(500);

                    if (viewModel.IsTryOpening) continue;

                    await new MessageDialog(exc.ToString(), "LeavingBackground").ShowAsync();

                    builder.WithService(audio);
                    Frame.Navigate(typeof(SettingsPage), builder);

                    break;
                }
            }

            viewModel.IsTryOpening = false;
        }

        private async void Application_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            if (viewModel.AudioService?.Base is IMqttAudioClient client && client.IsOpen)
            {
                try
                {
                    await client.CloseAsync();
                }
                catch (Exception exc)
                {
                    await new MessageDialog(exc.ToString(), "EnteredBackground").ShowAsync();

                    builder.WithService(client);

                    Frame.Navigate(typeof(SettingsPage), builder);
                }
            }
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
            await new MessageDialog(App.CreateTime.ToString()).ShowAsync();

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
