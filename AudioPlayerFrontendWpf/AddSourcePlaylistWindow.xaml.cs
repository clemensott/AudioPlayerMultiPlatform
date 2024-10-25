using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.FileSystem;
using AudioPlayerFrontend.Join;
using AudioPlayerFrontend.ViewModels;
using StdOttStandard.Converter.MultipleInputs;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Interaktionslogik für AddSourcePlaylistWindow.xaml
    /// </summary>
    public partial class AddSourcePlaylistWindow : Window
    {
        private readonly IUpdateLibraryService updateLibraryService;
        private readonly AddSourcePlaylistViewModel viewModel;

        public AddSourcePlaylistWindow(string[] sources, AudioServices audioServices)
        {
            InitializeComponent();

            updateLibraryService = audioServices.GetUpdateLibraryService();
            viewModel = new AddSourcePlaylistViewModel(audioServices);
            viewModel.AppendSources = true;
            viewModel.Sources = string.Join("\n", sources);

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
            string[] newPaths = viewModel.Sources?.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToArray();

            if (viewModel.NewPlaylist)
            {
                FileMediaSources fileMediaSources = FileMediaSourcesHelper.ExtractFileMediaSources(newPaths);
                Song[] songs = await updateLibraryService.ReloadSourcePlaylist(fileMediaSources);
                Playlist newPlaylist = new Playlist(Guid.NewGuid(), PlaylistType.SourcePlaylist, viewModel.Name,
                    viewModel.Shuffle, viewModel.Loop, 1, TimeSpan.Zero, TimeSpan.Zero, null, null, songs,
                    fileMediaSources, null, DateTime.Now, DateTime.Now);

                await viewModel.PlaylistsRepo.SendInsertPlaylist(newPlaylist, null);
                await viewModel.LibraryRepo.SendCurrentPlaylistIdChange(newPlaylist.Id);
            }
            else
            {
                PlaylistInfo selectedPlaylistInfo = (PlaylistInfo)lbxPlaylists.SelectedItem;
                string[] newAllPaths;
                if (viewModel.AppendSources)
                {
                    Playlist playlist = await viewModel.PlaylistsRepo.GetPlaylist(selectedPlaylistInfo.Id);
                    IEnumerable<string> existingPaths = UpdateLibraryService.LoadAllFilePaths(playlist.FileMediaSources);
                    newAllPaths = existingPaths.Concat(newPaths).Distinct().ToArray();
                }
                else
                {
                    newAllPaths = newPaths.ToArray();
                }

                FileMediaSources fileMediaSources = FileMediaSourcesHelper.ExtractFileMediaSources(newAllPaths);
                await viewModel.PlaylistsRepo.SendFileMedisSourcesChange(selectedPlaylistInfo.Id, fileMediaSources);
                await updateLibraryService.UpdateSourcePlaylist(selectedPlaylistInfo.Id);
            }

            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
