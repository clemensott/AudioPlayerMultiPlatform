using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using StdOttStandard.AsyncResult;
using StdOttUwp.Converters;
using System;
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
        private AsyncResultS<ServiceBuilder> result;

        public ServiceBuilder ServiceBuilder
        {
            get => serviceBuilder;
            private set
            {
                serviceBuilder = null;

                serverPortConverter.Convert(value.ServerPort);
                clientPortConverter.Convert(value.ClientPort);

                tbxServerAddress.Text = value.ServerAddress ?? string.Empty;

                if (value.BuildServer) tbxPort.Text = serverPortConverter.Text;
                else if (value.BuildClient) tbxPort.Text = clientPortConverter.Text;

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
            result = (AsyncResultS<ServiceBuilder>)e.Parameter;
            ServiceBuilder = result.Input;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (!result.HasResult) result.SetValue(null);
        }

        private object ShuffleConverter_ConvertEvent(object value, Type targetType, object parameter, string language)
        {
            return value ?? parameter;
        }

        private object ShuffleConverter_ConvertBackEvent(object value, Type targetType, object parameter, string language)
        {
            return parameter.Equals(value) ? (OrderType?)null : (OrderType)value;
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
            if (ServiceBuilder != null) ServiceBuilder.Shuffle = null;
        }

        private void CbxSearchShuffle_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ServiceBuilder != null) ServiceBuilder.IsSearchShuffle = null;
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

        private void AbbOk_Click(object sender, RoutedEventArgs e)
        {
            result.SetValue(serviceBuilder);
            Frame.GoBack();
        }

        private void AbbCancel_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
