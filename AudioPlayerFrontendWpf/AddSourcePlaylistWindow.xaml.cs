using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using AudioPlayerBackend;
using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.FileSystem;
using AudioPlayerFrontend.ViewModels;
using StdOttStandard.Converter.MultipleInputs;
using StdOttStandard.Linq;

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
            string[] paths = tbxSources.Text?.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToArray();
            Library library = await viewModel.LibraryRepo.GetLibrary();
            var fileMediaSourceRoots = library.FileMediaSourceRoots.ToNotNull();
            (IList<FileMediaSource> newSoruces, IList<FileMediaSourceRoot> newRoots) =
                FileMediaSourcesHelper.ExtractFileMediaSources(paths, fileMediaSourceRoots);

            if ((bool)micNewPlaylist.Output)
            {
                FileMediaSourceRoot[] newAllRoots = fileMediaSourceRoots.ToNotNull().Concat(newRoots).ToArray();
                FileMediaSource[] fileMediaSources = newSoruces.ToArray();
                Song[] songs = await fileSystemService.ReloadSourcePlaylist(newAllRoots, fileMediaSources);
                Playlist newPlaylist = new Playlist(Guid.NewGuid(), PlaylistType.SourcePlaylist, viewModel.Name, viewModel.Shuffle, viewModel.Loop, 1, TimeSpan.Zero, TimeSpan.Zero, null, null, songs, newSoruces.ToArray());

                await viewModel.LibraryRepo.SendFileMediaSourceRootsChange(newAllRoots);
                await viewModel.PlaylistsRepo.SendInsertPlaylist(newPlaylist, -1);
                await viewModel.LibraryRepo.SendCurrentPlaylistIdChange(newPlaylist.Id);
            }
            else
            {
                PlaylistInfo selectedPlaylistInfo = (PlaylistInfo)lbxPlaylists.SelectedItem;
                FileMediaSourceRoot[] newAllRoots;
                FileMediaSource[] newFileMediaSources;
                if (rbnAppend.IsChecked == true)
                {
                    Playlist playlist = await viewModel.PlaylistsRepo.GetPlaylist(selectedPlaylistInfo.Id);
                    newFileMediaSources = playlist.FileMediaSources.Concat(newSoruces).ToArray();
                    newAllRoots = fileMediaSourceRoots.Concat(newRoots).ToArray();
                }
                else
                {
                    newFileMediaSources = newSoruces.ToArray();
                    newAllRoots = fileMediaSourceRoots
                        .Where(root => service.SourcePlaylists.SelectMany(p => p.FileMediaSources).Any(s => s.RootId == root.Id))
                        .ToArray();
                }

                await viewModel.LibraryRepo.SendFileMediaSourceRootsChange(newAllRoots);
                await viewModel.PlaylistsRepo.SendFileMedisSourcesChange(selectedPlaylistInfo.Id, newFileMediaSources);
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
