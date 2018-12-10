using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
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
        private ViewModel viewModel;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ServiceBuilder)
            {
                builder = e.Parameter as ServiceBuilder;
                builder.WithPlayer(new Join.Player());

                DataContext = viewModel = new ViewModel(await builder.Build());
            }
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
