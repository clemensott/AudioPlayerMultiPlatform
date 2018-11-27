using AudioPlayerBackend;
using StdOttFramework.Converters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AudioPlayerFrontendWpf
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private IntConverter serverPortConverter;
        private IntNullableConverter clientPortConverter;

        public ServiceBuilder ServiceBuilder { get; private set; }

        public HotKeysBuilder HotKeysBuilder { get; private set; }

        public SettingsWindow() : this(new Join.ServiceBuilder(), new HotKeysBuilder())
        {
        }

        public SettingsWindow(IAudioExtended service, HotKeys hotKeys)
            : this(new Join.ServiceBuilder().WithService(service), new HotKeysBuilder().WithHotKeys(hotKeys))
        {
        }

        public SettingsWindow(ServiceBuilder serviceBuilder, HotKeysBuilder hotKeysBuilder)
        {
            InitializeComponent();

            serverPortConverter = new IntConverter();
            clientPortConverter = new IntNullableConverter()
            {
                AutoParseNullOrWhiteSpace = true,
                NullOrWhiteSpaceValue = null
            };

            timMode.DataContext = ServiceBuilder = serviceBuilder;
            timHotKeys.DataContext = HotKeysBuilder = hotKeysBuilder;

            if (serviceBuilder.BuildServer) tbxPort.Text = serverPortConverter.Convert(serviceBuilder.ServerPort);
            else if (serviceBuilder.BuildClient) tbxPort.Text = clientPortConverter.Convert(serviceBuilder.ClientPort);
            else tbxPort.Text = "1884";

            if (string.IsNullOrWhiteSpace(ServiceBuilder.ServerAddress)) ServiceBuilder.ServerAddress = "127.0.0.1";
        }

        private void RbnStandalone_Checked(object sender, RoutedEventArgs e)
        {
            ServiceBuilder.WithStandalone();
        }

        private void RbnServer_Checked(object sender, RoutedEventArgs e)
        {
            ServiceBuilder.WithServer(serverPortConverter.Value);
        }

        private void RbnClient_Checked(object sender, RoutedEventArgs e)
        {
            ServiceBuilder.WithClient(tbxServerAddress.Text, clientPortConverter.Value);
        }

        private void TbxPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ServiceBuilder == null) return;

            ServiceBuilder.ServerPort = serverPortConverter.ConvertBack(tbxPort.Text);
            ServiceBuilder.ClientPort = clientPortConverter.ConvertBack(tbxPort.Text);
        }

        private void CbxPlay_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            cbxPlay.IsChecked = ServiceBuilder.Play = null;
        }

        private void SldVolume_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.Volume = null;
        }

        private void CbxStreaming_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.IsStreaming = null;
        }

        private void SldClientVolume_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.ClientVolume = null;
        }

        private void CbxAllShuffle_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.IsAllShuffle = null;
        }

        private void CbxSearchShuffle_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.IsSearchShuffle = null;
        }

        private void CbxOnlySearch_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.IsOnlySearch = null;
        }
    }
}
