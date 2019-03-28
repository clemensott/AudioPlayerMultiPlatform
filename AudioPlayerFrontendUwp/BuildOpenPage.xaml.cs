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
using AudioPlayerBackend;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class BuildOpenPage : Page
    {
        private BuildStatusToken statusToken;

        public BuildOpenPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            statusToken = (BuildStatusToken) e.Parameter;

            base.OnNavigatedTo(e);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            statusToken.End(BuildEndedType.Canceled);
        }

        private async void BuildOpenPage_Loaded(object sender, RoutedEventArgs e)
        {
            await statusToken.Task;

            Frame.GoBack();
        }
    }
}
