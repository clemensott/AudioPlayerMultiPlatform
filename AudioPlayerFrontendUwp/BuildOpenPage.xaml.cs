using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using AudioPlayerBackend;
using AudioPlayerBackend.Player;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class BuildOpenPage : Page
    {
        private ServiceBuild build;

        public BuildOpenPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = build = (ServiceBuild)e.Parameter;

            base.OnNavigatedTo(e);

            tblFrameStack.Text = string.Join("\r\n", Frame.BackStack.Select(s => s.SourcePageType.FullName));
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            build.Cancel();
        }

        private async void BuildOpenPage_Loaded(object sender, RoutedEventArgs e)
        {
            await build.CommunicatorToken.ResultTask;

            await build.CompleteToken.EndTask;

            Frame.GoBack();
        }

        private async void AbbPrevious_Click(object sender, RoutedEventArgs e)
        {
            await build.SetPreviousSong();
        }

        private async void AbbPlay_Click(object sender, RoutedEventArgs e)
        {
            await build.SetPlayState(PlaybackState.Playing);
        }

        private async void AbbPause_Click(object sender, RoutedEventArgs e)
        {
            await build.SetPlayState(PlaybackState.Paused);
        }

        private async void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            await build.SetNextSong();
        }
    }
}
