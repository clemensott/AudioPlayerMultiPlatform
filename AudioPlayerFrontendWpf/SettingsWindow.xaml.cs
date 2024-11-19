using StdOttStandard.Linq;
using StdOttFramework.Converters;
using System.Windows;
using System.Windows.Controls;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using System.Linq;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly IntConverter serverPortConverter;
        private readonly IntNullableConverter clientPortConverter;

        public AudioServicesBuildConfig ServiceConfig { get; private set; }

        public HotKeysBuilder HotKeysBuilder { get; private set; }

        public SettingsWindow(AudioServicesBuildConfig serviceConfig, HotKeysBuilder hotKeysBuilder)
        {
            InitializeComponent();

            serverPortConverter = new IntConverter();
            clientPortConverter = new IntNullableConverter()
            {
                AutoParseNullOrWhiteSpace = true,
                NullOrWhiteSpaceValue = null
            };

            timMode.DataContext = ServiceConfig = serviceConfig;
            timHotKeys.DataContext = HotKeysBuilder = hotKeysBuilder;

            if (serviceConfig.BuildServer) tbxPort.Text = serverPortConverter.Convert(serviceConfig.ServerPort);
            else if (serviceConfig.BuildClient) tbxPort.Text = clientPortConverter.Convert(serviceConfig.ClientPort);

            lbxDefaultUpdateRoots.ItemsSource = serviceConfig.DefaultUpdateRoots;
        }

        private void RbnStandalone_Checked(object sender, RoutedEventArgs e)
        {
            ServiceConfig.WithStandalone();
        }

        private void RbnServer_Checked(object sender, RoutedEventArgs e)
        {
            ServiceConfig.WithServer(serverPortConverter.Value);
        }

        private void RbnClient_Checked(object sender, RoutedEventArgs e)
        {
            ServiceConfig.WithClient(tbxServerAddress.Text, clientPortConverter.Value);
        }

        private void TbxPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ServiceConfig == null) return;

            ServiceConfig.ServerPort = serverPortConverter.ConvertBack(tbxPort.Text);
            ServiceConfig.ClientPort = clientPortConverter.ConvertBack(tbxPort.Text);
        }

        private void LbxDefaultUpdateRoots_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lbxDefaultUpdateRoots.SelectedIndex = -1;
        }

        private void DefaultUpdateRootControl_ValueChanged(object sender, FileMediaSourceRootInfo e)
        {
            int index = lbxDefaultUpdateRoots.ItemsSource.ToNotNull().IndexOf(((FrameworkElement)sender).DataContext);
            if (index < 0) return;

            FileMediaSourceRootInfo[] newDefaultRoots = ServiceConfig.DefaultUpdateRoots.ToArray();
            newDefaultRoots[index] = e;
            ServiceConfig.WithDefaultUpdateRoots(newDefaultRoots);
        }

        private void BtnAddDefaultUpdateRoot_Click(object sender, RoutedEventArgs e)
        {
            ServiceConfig.WithDefaultUpdateRoots(ServiceConfig.DefaultUpdateRoots.ToNotNull()
                .ConcatParams(new FileMediaSourceRootInfo()).ToArray());
            lbxDefaultUpdateRoots.ItemsSource = ServiceConfig.DefaultUpdateRoots;

        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
