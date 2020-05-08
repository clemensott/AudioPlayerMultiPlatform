using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;
using StdOttStandard.Converter.MultipleInputs;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Interaktionslogik für BuildOpenWindow.xaml
    /// </summary>
    public partial class BuildOpenWindow : Window
    {
        private static BuildOpenWindow instance;

        public static BuildOpenWindow Current
        {
            get
            {
                if (instance == null) instance = new BuildOpenWindow();

                return instance;
            }
        }

        private ServiceBuild build, lastAwaitingBuild;

        public ServiceBuild Build
        {
            get => build;
            set => DataContext = build = value;
        }

        public BuildOpenWindow()
        {
            InitializeComponent();
        }

        public BuildOpenWindow(ServiceBuild build) : this()
        {
            Build = build;
        }

        protected override async void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == VisibilityProperty && Visibility == Visibility.Visible)
            {
                await AwaitBuild();
            }
        }

        protected override async void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            await AwaitBuild();
        }

        private async Task AwaitBuild()
        {
            if (Build == lastAwaitingBuild) return;
            lastAwaitingBuild = Build;

            await Task.Delay(100);
            BuildEndedType result = await lastAwaitingBuild.CompleteToken.EndTask;

            if (lastAwaitingBuild != Build) return;
            if (Visibility != Visibility.Visible) MessageBox.Show("BuildOpenWindow not visible!");
            if ((result == BuildEndedType.Successful || result == BuildEndedType.Settings) && Visibility == Visibility.Visible)
            {
                DialogResult = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Build.Cancel();
        }

        private void BtnOpeningSettings_Click(object sender, RoutedEventArgs e)
        {
            Build.Settings();
        }

        private void BtnException_Click(object sender, RoutedEventArgs e)
        {
            Exception exception = (Exception)micException.Output;

            if (exception != null)
            {
                MessageBox.Show(exception.ToString(), "Building audio service error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRetry_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            Build.Settings();
            DialogResult = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (DialogResult == true)
            {
                e.Cancel = true;
                Hide();
            }

            base.OnClosing(e);
        }

        private object MicException_Convert(object sender, MultiplesInputsConvert4EventArgs args)
        {
            return args.Input3 ?? args.Input2 ?? args.Input1 ?? args.Input0;
        }

        private async void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            await build.SetPreviousSong();
        }

        private async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            await build.SetPlayState(PlaybackState.Playing);
        }

        private async void TbnToggle_Checked(object sender, RoutedEventArgs e)
        {
            await build.SetToggle();
        }

        private async void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            await build.SetPlayState(PlaybackState.Paused);
        }

        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            await build.SetNextSong();
        }
    }
}
