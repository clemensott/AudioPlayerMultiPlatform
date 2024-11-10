using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Build;
using StdOttStandard.Linq;
using StdOttStandard.TaskCompletionSources;
using StdOttUwp.Converters;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private bool submit;
        private readonly IntConverter serverPortConverter;
        private readonly IntNullableConverter clientPortConverter;
        private AudioServicesBuildConfig audioServicesBuildConfig;
        private TaskCompletionSourceS<AudioServicesBuildConfig> result;

        public AudioServicesBuildConfig Config
        {
            get => audioServicesBuildConfig;
            private set
            {
                audioServicesBuildConfig = null;

                serverPortConverter.Convert(value.ServerPort);
                clientPortConverter.Convert(value.ClientPort);

                tbxServerAddress.Text = value.ServerAddress ?? string.Empty;

                if (value.BuildServer) tbxPort.Text = serverPortConverter.Text;
                else if (value.BuildClient) tbxPort.Text = clientPortConverter.Text;

                DataContext = audioServicesBuildConfig = value;

                UpdateMusicDefaultRootCheckboxes();
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
            result = (TaskCompletionSourceS<AudioServicesBuildConfig>)e.Parameter;
            Config = result.Input;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            result.SetResult(submit ? audioServicesBuildConfig : null);
        }

        private void RbnStandalone_Checked(object sender, RoutedEventArgs e)
        {
            Config?.WithStandalone();
        }

        private void RbnServer_Checked(object sender, RoutedEventArgs e)
        {
            tbxPort.Text = serverPortConverter.Text;

            Config?.WithServer(serverPortConverter.Value);
        }

        private void RbnClient_Checked(object sender, RoutedEventArgs e)
        {
            tbxPort.Text = clientPortConverter.Text;

            Config?.WithClient(tbxServerAddress.Text, clientPortConverter.Value);
        }

        private void TbxPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Config == null) return;

            Config.ServerPort = serverPortConverter.ConvertBack(tbxPort.Text);
            Config.ClientPort = clientPortConverter.ConvertBack(tbxPort.Text);
        }

        private static bool IsMusicDefaultRoot(FileMediaSourceRootInfo defaultRoot)
        {
            return defaultRoot.PathType == FileMediaSourceRootPathType.KnownFolder
                && defaultRoot.Path == KnownFolderId.MusicLibrary.ToString();
        }

        private void UpdateMusicDefaultRootCheckboxes()
        {
            if (Config.DefaultUpdateRoots.ToNotNull().TryFirst(IsMusicDefaultRoot, out FileMediaSourceRootInfo musicDefaultRoot))
            {
                cbxMusicDefaultRoot.IsChecked = true;
                cbxMusicWithSubFolders.IsChecked = musicDefaultRoot.UpdateType.HasFlag(FileMediaSourceRootUpdateType.Folders);
            }
            else
            {
                cbxMusicDefaultRoot.IsChecked = false;
                cbxMusicWithSubFolders.IsChecked = false;
            }
        }

        private void CbxMusicDefaultRoot_Checked(object sender, RoutedEventArgs e)
        {
            FileMediaSourceRootInfo musicDefaultRoot = new FileMediaSourceRootInfo(
                FileMediaSourceRootUpdateType.Songs | FileMediaSourceRootUpdateType.Folders,
                KnownFolders.MusicLibrary.DisplayName,
                FileMediaSourceRootPathType.KnownFolder,
                KnownFolderId.MusicLibrary.ToString()
            );
            Config?.WithDefaultUpdateRoots(Config.DefaultUpdateRoots.ToNotNull().ConcatParams(musicDefaultRoot).ToArray());
        }

        private void CbxMusicDefaultRoot_Unchecked(object sender, RoutedEventArgs e)
        {
            Config?.WithDefaultUpdateRoots(Config.DefaultUpdateRoots.ToNotNull().Where(r => !IsMusicDefaultRoot(r)).ToArray());
        }

        private void SetMusicDefaultRootUpdateType(FileMediaSourceRootUpdateType updateType)
        {
            Config?.WithDefaultUpdateRoots(Config.DefaultUpdateRoots.ToNotNull().Select(r => IsMusicDefaultRoot(r)
                ? new FileMediaSourceRootInfo(updateType, r.Name, r.PathType, r.Path)
                : r
            ).ToArray());
        }

        private void CbxMusicWithSubFolders_Checked(object sender, RoutedEventArgs e)
        {
            SetMusicDefaultRootUpdateType(FileMediaSourceRootUpdateType.Songs | FileMediaSourceRootUpdateType.Folders);
        }

        private void CbxMusicWithSubFolders_Unchecked(object sender, RoutedEventArgs e)
        {
            SetMusicDefaultRootUpdateType(FileMediaSourceRootUpdateType.Songs);
        }

        private void AbbOk_Click(object sender, RoutedEventArgs e)
        {
            submit = true;
            Frame.GoBack();
        }

        private void AbbCancel_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
