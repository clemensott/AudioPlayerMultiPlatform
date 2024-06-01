using AudioPlayerBackend.Audio;
using AudioPlayerFrontend.Join;
using StdOttFramework.Converters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AudioPlayerBackend.Build;
using StdOttStandard.Converter.MultipleInputs;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private IntConverter serverPortConverter;
        private IntNullableConverter clientPortConverter;

        public AudioServicesBuildConfig ServiceBuilder { get; private set; }

        public HotKeysBuilder HotKeysBuilder { get; private set; }

        public SettingsWindow(AudioServicesBuildConfig serviceBuilder, HotKeysBuilder hotKeysBuilder)
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

            if (!serviceBuilder.IsSearchShuffle.HasValue) cbxSearchShuffle.IsChecked = null;
            if (!serviceBuilder.Play.HasValue) cbxPlay.IsChecked = null;
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

        private object MicVolume_ConvertRef(object sender, MultiplesInputsConvert3EventArgs args)
        {
            if (args.Input1 == null) args.Input1 = false;
            if (args.Input2 == null) args.Input2 = 1d;

            switch (args.ChangedValueIndex)
            {
                case 0:
                    if (args.Input0 is float)
                    {
                        args.Input1 = true;
                        args.Input2 = (double)(float)args.Input0;
                    }
                    else args.Input1 = false;
                    break;

                case 1:
                case 2:
                    args.Input0 = true.Equals(args.Input1) ? (float?)(double)args.Input2 : null;
                    break;
            }

            return null;
        }

        private void CbxPlay_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            cbxPlay.IsChecked = ServiceBuilder.Play = null;
        }

        private void CbxStreaming_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.IsStreaming = null;
        }

        private void CbxAllShuffle_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.Shuffle = null;
        }

        private void CbxSearchShuffle_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ServiceBuilder.IsSearchShuffle = null;
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
