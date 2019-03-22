using AudioPlayerBackend;
using StdOttUwp.Converters;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private IntConverter serverPortConverter;
        private IntNullableConverter clientPortConverter;
        private ServiceBuilder serviceBuilder;

        public ServiceBuilder ServiceBuilder
        {
            get => serviceBuilder;
            set
            {
                serviceBuilder = null;

                serverPortConverter.Convert(value.ServerPort);
                clientPortConverter.Convert(value.ClientPort);

                tbxServerAddress.Text = value.ServerAddress ?? string.Empty;

                if (value.BuildServer) tbxPort.Text = serverPortConverter.Text;
                else if (value.BuildClient) tbxPort.Text = clientPortConverter.Text;
                //else tbxPort.Text = "1884";

                if (string.IsNullOrWhiteSpace(value.ServerAddress)) value.ServerAddress = "127.0.0.1";

                DataContext = serviceBuilder = value;
            }
        }

        public SettingsPage()
        {
            this.InitializeComponent();

            serverPortConverter = new IntConverter();
            clientPortConverter = new IntNullableConverter()
            {
                AutoParseNullOrWhiteSpace = true,
                NullOrWhiteSpaceValue = null
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ServiceBuilder) ServiceBuilder = (ServiceBuilder)e.Parameter;
        }

        private void RbnStandalone_Checked(object sender, RoutedEventArgs e)
        {
            ServiceBuilder?.WithStandalone();
        }

        private void RbnServer_Checked(object sender, RoutedEventArgs e)
        {
            tbxPort.Text = serverPortConverter.Text;

            ServiceBuilder?.WithServer(serverPortConverter.Value);
        }

        private void RbnClient_Checked(object sender, RoutedEventArgs e)
        {
            tbxPort.Text = clientPortConverter.Text;

            ServiceBuilder?.WithClient(tbxServerAddress.Text, clientPortConverter.Value);
        }

        private void TbxPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ServiceBuilder == null) return;

            ServiceBuilder.ServerPort = serverPortConverter.ConvertBack(tbxPort.Text);
            ServiceBuilder.ClientPort = clientPortConverter.ConvertBack(tbxPort.Text);
        }

        private void CbxAllShuffle_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ServiceBuilder != null) ServiceBuilder.IsAllShuffle = null;
        }

        private void CbxSearchShuffle_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ServiceBuilder != null) ServiceBuilder.IsSearchShuffle = null;
        }

        private void CbxOnlySearch_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ServiceBuilder != null) ServiceBuilder.IsOnlySearch = null;
        }

        private void CbxPlay_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ServiceBuilder != null) cbxPlay.IsChecked = ServiceBuilder.Play = null;
        }

        private void SldVolume_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ServiceBuilder != null) ServiceBuilder.Volume = null;
        }

        private void CbxStreaming_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ServiceBuilder != null) ServiceBuilder.IsStreaming = null;
        }

        private void AbbGoBack_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
