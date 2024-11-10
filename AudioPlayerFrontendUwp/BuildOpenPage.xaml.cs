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
using Windows.UI.Core;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class BuildOpenPage : Page
    {
        private bool isPageClosed = false;
        private AudioServicesHandler audioServicesHandler;

        private AudioServicesBuilder Builder => DataContext as AudioServicesBuilder;

        public BuildOpenPage()
        {
            AudioPlayerBackend.Logs.Log("BuildOpenPage1");
            this.InitializeComponent();
            AudioPlayerBackend.Logs.Log("BuildOpenPage2");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            AudioPlayerBackend.Logs.Log("BuildOpenPage.OnNavigatedTo1");
            audioServicesHandler = (AudioServicesHandler)e.Parameter;
            audioServicesHandler.AddServiceBuildListener(AudioServicesHandler_ServicesBuild);

            IEnumerable<string> frames = Frame.BackStack.Select(s => s.SourcePageType.FullName);
            tblFrameStack.Text = string.Join("\r\n", frames);

            AudioPlayerBackend.Logs.Log("BuildOpenPage.OnNavigatedTo2");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            isPageClosed = true;
            audioServicesHandler.ServicesBuild -= AudioServicesHandler_ServicesBuild;
        }

        private async void AudioServicesHandler_ServicesBuild(object sender, AudioServicesBuilder e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await SetDataContext(e));
        }

        private async Task SetDataContext(AudioServicesBuilder dataContext)
        {
            AudioPlayerBackend.Logs.Log("BuildOpenPage.SetDataContext1");
            if (dataContext == null) return;

            // only show all options if opening takes to long
            await Task.Delay(TimeSpan.FromSeconds(5));
            if (!isPageClosed) DataContext = dataContext;
            AudioPlayerBackend.Logs.Log("BuildOpenPage.SetDataContext2");
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
            Exception exception = audioServicesHandler?.Builder?.CompleteToken.Exception;

            if (exception != null)
            {
                await new MessageDialog(exception.ToString(), "Building audio service error").ShowAsync();
            }
        }
    }
}
