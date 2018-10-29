using AudioPlayerBackendLib;
using StdOttWpfLib.Converters;
using System.Linq;
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

        public SettingsWindow() : this(new ServiceBuilder(), new HotKeysBuilder())
        {
        }

        public SettingsWindow(IAudioExtended service, HotKeys hotKeys)
            : this(new ServiceBuilder().WithService(service), new HotKeysBuilder().WithHotKeys(hotKeys))
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

            rbnStandalone.IsChecked = serviceBuilder.BuildStandalone;
            rbnServer.IsChecked = serviceBuilder.BuildServer;
            rbnClient.IsChecked = serviceBuilder.BuildClient;

            if (serviceBuilder.BuildServer) tbxPort.Text = serverPortConverter.Convert(serviceBuilder.ServerPort);
            else if (serviceBuilder.BuildClient) tbxPort.Text = clientPortConverter.Convert(serviceBuilder.ClientPort);

            if (serviceBuilder.Volume.HasValue) sldVolume.Value = serviceBuilder.Volume.Value;
            if (serviceBuilder.ClientVolume.HasValue) sldClientVolume.Value = serviceBuilder.ClientVolume.Value;

            tbxMediaSources.Text = serviceBuilder.MediaSources != null ?
                string.Join("\r\n", serviceBuilder.MediaSources) : string.Empty;
        }

        private void RbnStandalone_Checked(object sender, RoutedEventArgs e)
        {
            ServiceBuilder.WithStandalone();
        }

        private void RbnServer_Checked(object sender, RoutedEventArgs e)
        {
            ServiceBuilder.WithServer(serverPortConverter.CurrentValue);
        }

        private void RbnClient_Checked(object sender, RoutedEventArgs e)
        {
            ServiceBuilder.WithClient(tbxServerAddress.Text, clientPortConverter.CurrentValue);
        }

        private void TbxPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            ServiceBuilder.ServerPort = serverPortConverter.ConvertBack(tbxPort.Text);
            ServiceBuilder.ClientPort = clientPortConverter.ConvertBack(tbxPort.Text);
        }

        private void CbxPlay_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            cbxPlay.IsChecked = ServiceBuilder.Play = null;
        }

        private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            ServiceBuilder.Volume = (float)e.NewValue;
        }

        private void SldVolume_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.Volume = null;
        }

        private void CbxStreaming_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            cbxStreaming.IsChecked = ServiceBuilder.IsStreaming = null;
        }

        private void SldClientVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            ServiceBuilder.ClientVolume = (float)e.NewValue;
        }

        private void SldClientVolume_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.ClientVolume = null;
        }

        private void TbxMediaSources_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (tbxMediaSources.Text.Length == 0) ServiceBuilder.MediaSources = null;
            else ServiceBuilder.MediaSources = tbxMediaSources.Text.Replace("\r", "").Split('\n').ToArray();
        }
    }
}
