using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using StdOttStandard.TaskCompletionSources;
using StdOttStandard.Converter.MultipleInputs;
using StdOttUwp.Converters;
using System;
using System.Threading.Tasks;
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
        private TaskCompletionSourceS<ServiceBuilder> result;

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

                if (!serviceBuilder.IsSearchShuffle.HasValue) cbxSearchShuffle.IsChecked = null;
                if (!serviceBuilder.Play.HasValue) cbxPlay.IsChecked = null;
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
            result = (TaskCompletionSourceS<ServiceBuilder>)e.Parameter;
            serviceBuilder = result.Input;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (!result.Task.IsCompleted) result.SetResult(null);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ServiceBuilder = serviceBuilder;
        }

        private object ShuffleConverter_ConvertEvent(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return parameter;
            return (int)value;
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

        private async void Cbx_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            await Task.Delay(50);
            if (ServiceBuilder != null) ((CheckBox)sender).IsChecked = null;
        }

        private object MicVolume_ConvertRef(object sender, MultiplesInputsConvert3EventArgs args)
        {
            if (args.Input2 == null) args.Input2 = 1;

            switch (args.ChangedValueIndex)
            {
                case 0:
                    if (args.Input0 is float)
                    {
                        args.Input1 = true;
                        args.Input2 = args.Input0;
                    }
                    else args.Input1 = false;
                    break;

                case 1:
                case 2:
                    args.Input0 = true.Equals(args.Input1) ? args.Input2 : null;
                    break;
            }

            return null;
        }

        private void CbxStreaming_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ServiceBuilder != null) ServiceBuilder.IsStreaming = null;
        }

        private void AbbOk_Click(object sender, RoutedEventArgs e)
        {
            result.SetResult(serviceBuilder);
            Frame.GoBack();
        }

        private void AbbCancel_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void CbxPlay_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (ServiceBuilder != null) cbxPlay.IsChecked = ServiceBuilder.Play = null;
            e.Handled = true;
        }
    }
}
