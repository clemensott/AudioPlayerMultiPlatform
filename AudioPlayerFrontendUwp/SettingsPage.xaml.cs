using AudioPlayerBackend;
using StdOttUwp.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
            get { return serviceBuilder; }
            set
            {
                DataContext = serviceBuilder = value;

                if (serviceBuilder.BuildServer) tbxPort.Text = serverPortConverter.Convert(serviceBuilder.ServerPort);
                else if (serviceBuilder.BuildClient) tbxPort.Text = clientPortConverter.Convert(serviceBuilder.ClientPort);
                else tbxPort.Text = "1884";

                if (string.IsNullOrWhiteSpace(ServiceBuilder.ServerAddress)) ServiceBuilder.ServerAddress = "127.0.0.1";
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

        private void CbxAllShuffle_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ServiceBuilder.IsAllShuffle = null;
        }

        private void CbxSearchShuffle_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ServiceBuilder.IsSearchShuffle = null;
        }

        private void CbxOnlySearch_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ServiceBuilder.IsOnlySearch = null;
        }

        private void CbxPlay_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            cbxPlay.IsChecked = ServiceBuilder.Play = null;
        }

        private void SldVolume_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ServiceBuilder.Volume = null;
        }

        private void CbxStreaming_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ServiceBuilder.IsStreaming = null;
        }

        private void SldClientVolume_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ServiceBuilder.ClientVolume = null;
        }

        private void AbbGoBack_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
