using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using AudioPlayerBackend.Player;
using StdOttStandard.Converter.MultipleInputs;
using System.ComponentModel;
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
        private ServiceHandler serviceHandler;

        public BuildOpenPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            serviceHandler = (ServiceHandler)e.Parameter;
            serviceHandler.PropertyChanged += ServiceHandler_PropertyChanged;

            IEnumerable<string> frames = Frame.BackStack.Select(s => s.SourcePageType.FullName);
            tblFrameStack.Text = string.Join("\r\n", frames);

            await SetDataContext(serviceHandler.ServiceOpenBuild);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            serviceHandler.PropertyChanged -= ServiceHandler_PropertyChanged;
        }

        private async void ServiceHandler_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(serviceHandler.ServiceOpenBuild))
            {
                await SetDataContext(serviceHandler.ServiceOpenBuild);
            }
        }

        private async Task SetDataContext(ServiceBuild dataContext)
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
            serviceHandler.ServiceOpenBuild.Settings();
        }

        private async void BtnException_Click(object sender, RoutedEventArgs e)
        {
            Exception exception = (Exception)micException.Output;

            if (exception != null)
            {
                await new MessageDialog(exception.ToString(), "Building audio service error").ShowAsync();
            }
        }

        private async void AbbPrevious_Click(object sender, RoutedEventArgs e)
        {
            await serviceHandler.ServiceOpenBuild.SetPreviousSong();
        }

        private async void AbbPlay_Click(object sender, RoutedEventArgs e)
        {
            await serviceHandler.ServiceOpenBuild.SetPlayState(PlaybackState.Playing);
        }

        private async void AbbPause_Click(object sender, RoutedEventArgs e)
        {
            await serviceHandler.ServiceOpenBuild.SetPlayState(PlaybackState.Paused);
        }

        private async void AtbToggle_Checked(object sender, RoutedEventArgs e)
        {
            await serviceHandler.ServiceOpenBuild.SetToggle();
        }

        private async void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            await serviceHandler.ServiceOpenBuild.SetNextSong();
        }
    }
}
