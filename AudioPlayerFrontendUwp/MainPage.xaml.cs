using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Threading.Tasks;
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
        private ServiceBuilder builder;
        private Task<IAudioExtended> buildTask;
        private ViewModel viewModel;

        public MainPage()
        {
            this.InitializeComponent();

            System.Diagnostics.Debug.WriteLine("MainPageCtor");
            Application.Current.EnteredBackground += Current_EnteredBackground;
            Application.Current.LeavingBackground += Current_LeavingBackground;
        }

        private async void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Leaving: " + (viewModel == null));
            if (viewModel?.Parent is IMqttAudioClient client && !client.IsOpen) await client.OpenAsync();
        }

        private async void Current_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Entering: " + (viewModel == null));
            if (viewModel?.Parent is IMqttAudioClient client && client.IsOpen) await client.CloseAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ServiceBuilder)
            {
                builder = e.Parameter as ServiceBuilder;

                if (builder.Player == null) builder.WithPlayer(new Join.Player());

                buildTask = builder.Build();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await buildTask;

                DataContext = viewModel = new ViewModel(buildTask.Result);
            }
            catch
            {
                DataContext = viewModel = null;
            }

            if (viewModel == null) Frame.Navigate(typeof(SettingsPage), builder);
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
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
            builder.WithService(viewModel.Parent);

            Frame.Navigate(typeof(SettingsPage), builder);
        }
    }
}
