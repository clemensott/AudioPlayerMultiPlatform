using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using StdOttStandard.Converter.MultipleInputs;
using System.Threading.Tasks;
using AudioPlayerBackend.Build;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class BuildOpenPage : Page
    {
        private AudioServicesHandler audioServicesHandler;
        
        private AudioServicesBuilder Builder => DataContext as AudioServicesBuilder;

        public BuildOpenPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            audioServicesHandler = (AudioServicesHandler)e.Parameter;
            audioServicesHandler.ServicesBuild += AudioServicesHandler_ServicesBuild;

            IEnumerable<string> frames = Frame.BackStack.Select(s => s.SourcePageType.FullName);
            tblFrameStack.Text = string.Join("\r\n", frames);

            await SetDataContext(audioServicesHandler.Builder);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            audioServicesHandler.ServicesBuild -= AudioServicesHandler_ServicesBuild;
        }

        private async void AudioServicesHandler_ServicesBuild(object sender, AudioServicesBuilder e)
        {
            await SetDataContext(e);
        }

        private async Task SetDataContext(AudioServicesBuilder dataContext)
        {
            if (dataContext == null) return;

            // only show all options if opening takes to long
            await Task.Delay(TimeSpan.FromSeconds(5));
            DataContext = dataContext;
        }

        private object MicException_Convert(object sender, MultiplesInputsConvert4EventArgs args)
        {
            return args.Input3 ?? args.Input2 ?? args.Input1 ?? args.Input0;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            Builder.Settings();
        }

        private async void BtnException_Click(object sender, RoutedEventArgs e)
        {
            Exception exception = (Exception)micException.Output;

            if (exception != null)
            {
                await new MessageDialog(exception.ToString(), "Building audio service error").ShowAsync();
            }
        }
    }
}
