using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using AudioPlayerBackend;
using AudioPlayerBackend.Communication;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class BuildOpenPage : Page
    {
        private ServiceBuild build;
        private ICommunicator communicator;

        public BuildOpenPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            build = (ServiceBuild)e.Parameter;
            cbrBottom.Visibility = Visibility.Collapsed;

            base.OnNavigatedTo(e);

            tblFrameStack.Text = string.Join("\r\n", Frame.BackStack.Select(s => s.SourcePageType.FullName));
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            build.Cancel();
        }

        private async void BuildOpenPage_Loaded(object sender, RoutedEventArgs e)
        {
            communicator = await build.CommunicatorToken.ResultTask;
            cbrBottom.Visibility = Visibility.Visible;

            await build.CompleteToken.EndTask;

            Frame.GoBack();
        }

        private async void AbbPrevious_Click(object sender, RoutedEventArgs e)
        {
            await communicator.PreviousSong();
        }

        private async void AbbPlay_Click(object sender, RoutedEventArgs e)
        {
            await communicator.PlaySong();
        }

        private async void AbbPause_Click(object sender, RoutedEventArgs e)
        {
            await communicator.PauseSong();
        }

        private async void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            await communicator.NextSong();
        }
    }
}
