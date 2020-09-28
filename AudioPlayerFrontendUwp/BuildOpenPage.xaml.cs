using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;
using StdOttStandard.Converter.MultipleInputs;
using StdOttUwp.BackPress;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class BuildOpenPage : Page
    {
        private ServiceBuild build;

        public BuildOpenPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = build = (ServiceBuild)e.Parameter;

            base.OnNavigatedTo(e);

            IEnumerable<string> frames = Frame.BackStack.Select(s => s.SourcePageType.FullName);
            tblFrameStack.Text = string.Join("\r\n", frames);

            BackPressHandler.Current.BackPressed += BackPressHandler_BackPressed;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            BackPressHandler.Current.BackPressed += BackPressHandler_BackPressed;
        }

        private void BackPressHandler_BackPressed(object sender, BackPressEventArgs e)
        {
            e.Action = BackPressAction.Unhandled;
        }

        private object MicException_Convert(object sender, MultiplesInputsConvert4EventArgs args)
        {
            return args.Input3 ?? args.Input2 ?? args.Input1 ?? args.Input0;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            build.Settings();
        }

        private async void BtnException_Click(object sender, RoutedEventArgs e)
        {
            Exception exception = (Exception)micException.Output;

            if (exception != null)
            {
                await new MessageDialog(exception.ToString(), "Building audio service error").ShowAsync();
            }
        }

        private async void BuildOpenPage_Loaded(object sender, RoutedEventArgs e)
        {
            await build.CompleteToken.EndTask;

            Frame.GoBack();
        }

        private async void AbbPrevious_Click(object sender, RoutedEventArgs e)
        {
            await build.SetPreviousSong();
        }

        private async void AbbPlay_Click(object sender, RoutedEventArgs e)
        {
            await build.SetPlayState(PlaybackState.Playing);
        }

        private async void AbbPause_Click(object sender, RoutedEventArgs e)
        {
            await build.SetPlayState(PlaybackState.Paused);
        }

        private async void AtbToggle_Checked(object sender, RoutedEventArgs e)
        {
            await build.SetToggle();
        }

        private async void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            await build.SetNextSong();
        }
    }
}
