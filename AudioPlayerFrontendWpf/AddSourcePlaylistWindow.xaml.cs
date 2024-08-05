using System;
using System.IO;
using System.Linq;
using System.Windows;
using AudioPlayerBackend;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.FileSystem;
using AudioPlayerFrontend.ViewModels;
using StdOttStandard.Converter.MultipleInputs;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Interaktionslogik für AddSourcePlaylistWindow.xaml
    /// </summary>
    public partial class AddSourcePlaylistWindow : Window
    {
        private readonly IFileSystemService fileSystemService;
        private readonly AddSourcePlaylistViewModel viewModel;

        public AddSourcePlaylistWindow(string[] sources, AudioServices audioServices)
        {
            InitializeComponent();

            fileSystemService = AudioPlayerServiceProvider.Current.GetFileSystemService();
            viewModel = new AddSourcePlaylistViewModel(audioServices);
            viewModel.Sources = sources;

            try
            {
                viewModel.Name = sources.Length == 1 ?
                    Path.GetFileNameWithoutExtension(sources[0]) :
                    Path.GetFileName(Path.GetDirectoryName(sources[0]));
            }
            catch { }

            DataContext = viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await viewModel.Start();
        }

        private async void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            await viewModel.Dispose();
        }

        private object MicNewPlaylist_Convert(object sender, MultiplesInputsConvert2EventArgs args)
        {
            if (args.Input0 == null || args.Input1 == null) return false;

            int count = (int)args.Input0;
            bool isNewPlaylistChecked = (bool?)args.Input1 == true;

            return count == 0 || isNewPlaylistChecked;
        }

        private object MicOk_Convert(object sender, MultiplesInputsConvert3EventArgs args)
        {
            if (args.Input1 == null) return false;

            PlaylistInfo selectedPlaylist = (PlaylistInfo)args.Input0;
            bool isNewPlaylist = (bool)args.Input1;
            string newPlaylistName = (string)args.Input2;

            return isNewPlaylist ? !string.IsNullOrWhiteSpace(newPlaylistName) : selectedPlaylist != null;
        }

        private async void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string[] newPaths = tbxSources.Text?.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToArray();

            if ((bool)micNewPlaylist.Output)
            {
                FileMediaSources fileMediaSources = FileMediaSourcesHelper.ExtractFileMediaSources(newPaths);
                Song[] songs = await fileSystemService.ReloadSourcePlaylist(fileMediaSources);
                Playlist newPlaylist = new Playlist(Guid.NewGuid(), PlaylistType.SourcePlaylist, viewModel.Name,
                    viewModel.Shuffle, viewModel.Loop, 1, TimeSpan.Zero, TimeSpan.Zero, null, null, songs, fileMediaSources);

                await viewModel.PlaylistsRepo.SendInsertPlaylist(newPlaylist, -1);
                await viewModel.LibraryRepo.SendCurrentPlaylistIdChange(newPlaylist.Id);
            }
            else
            {
                PlaylistInfo selectedPlaylistInfo = (PlaylistInfo)lbxPlaylists.SelectedItem;
                string[] newAllPaths;
                if (rbnAppend.IsChecked == true)
                {
                    Playlist playlist = await viewModel.PlaylistsRepo.GetPlaylist(selectedPlaylistInfo.Id);
                    newAllPaths = playlist.FileMediaSources.Sources.Select(s => s.RelativePath).Concat(newPaths).ToArray();
                }
                else
                {
                    newAllPaths = newPaths.ToArray();
                }

                FileMediaSources fileMediaSources = FileMediaSourcesHelper.ExtractFileMediaSources(newAllPaths);
                await viewModel.PlaylistsRepo.SendFileMedisSourcesChange(selectedPlaylistInfo.Id, fileMediaSources);
                await fileSystemService.UpdateSourcePlaylist(selectedPlaylistInfo.Id);
            }

            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
