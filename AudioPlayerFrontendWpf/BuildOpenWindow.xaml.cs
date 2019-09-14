using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;

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

        private bool isAwaiting;
        private ServiceBuild build;

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
            try
            {
                System.Diagnostics.Debug.WriteLine("BeginAwait: {0}", isAwaiting);
                if (isAwaiting) return;
                isAwaiting = true;

                System.Diagnostics.Debug.WriteLine("DoAwait");
                await Task.Delay(100);
                BuildEndedType result = await Build.CompleteToken.EndTask;
                System.Diagnostics.Debug.WriteLine("AwatiedBuild: {0} | {1} | ", result, Visibility, DialogResult);

                if (Visibility != Visibility.Visible) MessageBox.Show("BuildOpenWindow not visible!");
                if ((result == BuildEndedType.Successful || result == BuildEndedType.Settings) && Visibility == Visibility.Visible)
                {
                    DialogResult = true;
                }
            }
            finally
            {
                isAwaiting = false;
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
                //System.Diagnostics.Debug.WriteLine("OnCloding1");
                e.Cancel = true;
                Hide();
                System.Diagnostics.Debug.WriteLine("OnCloding2");
            }

            base.OnClosing(e);
        }

        private object MicException_Convert(object input0, object input1, object input2, object input3, int changedInput)
        {
            if (input3 != null) return input3;
            if (input2 != null) return input2;
            if (input1 != null) return input1;

            return input0;
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
