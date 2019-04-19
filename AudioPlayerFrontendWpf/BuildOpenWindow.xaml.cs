using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using AudioPlayerBackend;

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

        private BuildStatusToken statusToken;

        public BuildStatusToken StatusToken
        {
            get => statusToken;
            set => DataContext = statusToken = value;
        }

        public BuildOpenWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        public BuildOpenWindow(BuildStatusToken statusToken) : this()
        {
            StatusToken = statusToken;
        }

        protected override async void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (IsLoaded) await AwaitBuildOrOpen();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            await AwaitBuildOrOpen();
        }

        private async Task AwaitBuildOrOpen()
        {
            await Task.Delay(100);
            BuildEndedType result = await StatusToken.Task;

            if (result == BuildEndedType.Successful || result == BuildEndedType.Settings) DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            StatusToken.End(BuildEndedType.Canceled);
        }

        private void BtnOpeningSettings_Click(object sender, RoutedEventArgs e)
        {
            StatusToken.End(BuildEndedType.Settings);
        }

        private void BtnException_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(StatusToken.Exception.ToString(),
                "Building audio service error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BtnRetry_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            StatusToken.End(BuildEndedType.Settings);
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
    }
}
