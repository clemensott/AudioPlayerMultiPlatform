using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking.Connectivity;
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

            try
            {
                if (builder.BuildClient) await WaitForNetworkConnection(networkConnectionTimeOut, networkConnectionMaxTime);

                await buildTask;

                DataContext = viewModel = new ViewModel(buildTask.Result);
            }
            catch
            {
                DataContext = viewModel = null;
            }

            if (viewModel == null) Frame.Navigate(typeof(SettingsPage), builder);
            else tbxSearchKey.IsEnabled = true;
        }

        private async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Application.Current.EnteredBackground -= Application_EnteredBackground;
            Application.Current.LeavingBackground -= Application_LeavingBackground;

            try
            {
                if (viewModel != null && viewModel.Base is IMqttAudio mqttService) await mqttService.CloseAsync();
            }
            catch { }
        }

        private async void Application_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            try
            {
                if (viewModel?.Base is IMqttAudioClient client && !client.IsOpen)
                {
                    await WaitForNetworkConnection(networkConnectionTimeOut, networkConnectionMaxTime);
                    await client.OpenAsync();
                }
            }
            catch (Exception exc)
            {
                await new MessageDialog(exc.ToString()).ShowAsync();

                builder.WithService(viewModel.Base);

                Frame.Navigate(typeof(SettingsPage), builder);
            }
        }

        private async void Application_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            try
            {
                if (viewModel?.Base is IMqttAudioClient client && client.IsOpen) await client.CloseAsync();
            }
            catch (Exception exc)
            {
                await new MessageDialog(exc.ToString()).ShowAsync();

                builder.WithService(viewModel.Base);

                Frame.Navigate(typeof(SettingsPage), builder);
            }
        }

        private async Task<bool> WaitForNetworkConnection(TimeSpan timeOut, TimeSpan maxTime)
        {
            //return true;

            DateTime startTime = DateTime.Now;

            while (true)
            {
                ConnectionProfile connectedProfile = NetworkInformation.GetInternetConnectionProfile();
                if (connectedProfile != null) return true;
                if (startTime + maxTime >= DateTime.Now) return false;

                await Task.Delay(timeOut);
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Reload();
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
            viewModel?.SetPreviousSong();
        }

        private void AbbPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;

            viewModel.PlayState = viewModel.PlayState == PlaybackState.Playing ? PlaybackState.Paused : PlaybackState.Playing;
        }

        private void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            viewModel?.SetNextSong();
        }

        private void AbbSettings_Click(object sender, RoutedEventArgs e)
        {
            builder.WithService(viewModel.Base);

            Frame.Navigate(typeof(SettingsPage), builder);
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
