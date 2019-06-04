using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using AudioPlayerBackend;
using AudioPlayerBackend.Communication;

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

        public BuildOpenWindow(ServiceBuild build)
        {
            InitializeComponent();

            Build = build;
        }

        //protected override async void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        //{
        //    base.OnPropertyChanged(e);

        //    if (e.Property == VisibilityProperty && Visibility == Visibility.Visible)
        //    {
        //        await AwaitBuild();
        //    }
        //}

        protected override async void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            await AwaitBuild();
        }

        private async Task AwaitBuild()
        {
            if (isAwaiting) return;
            isAwaiting = true;

            await Task.Delay(100);
            BuildEndedType result = await Build.CompleteToken.EndTask;

            if (result == BuildEndedType.Successful || result == BuildEndedType.Settings) DialogResult = true;

            isAwaiting = false;
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

        private object MicException_Convert(object input0, object input1, object input2, object input3, int changedInput)
        {
            if (input3 != null) return input3;
            if (input2 != null) return input2;
            if (input1 != null) return input1;

            return input0;
        }

        private async void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            await build.CommunicatorToken.Result.PreviousSong();
        }

        private async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            await build.CommunicatorToken.Result.PlaySong();
        }

        private async void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            await build.CommunicatorToken.Result.PauseSong();
        }

        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            await build.CommunicatorToken.Result.NextSong();
        }
    }
}
