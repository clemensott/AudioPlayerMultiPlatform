using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.FileSystem;
using AudioPlayerFrontendAvalonia.Join;
using AudioPlayerFrontendAvalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AudioPlayerFrontendAvalonia.Views;

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
            viewModel.Name =
                (sources.Length == 1
                    ? Path.GetFileNameWithoutExtension(sources[0])
                    : Path.GetFileName(Path.GetDirectoryName(sources[0]))) ?? string.Empty;
        }
        catch
        {
        }

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

    private async void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        string[] newPaths = viewModel.Sources.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToArray();

        if (viewModel.NewPlaylist)
        {
            FileMediaSources fileMediaSources = FileMediaSourcesHelper.ExtractFileMediaSources(newPaths);
            Song[] songs = await updateLibraryService.LoadSongs(fileMediaSources);
            Playlist newPlaylist = new Playlist(Guid.NewGuid(), PlaylistType.SourcePlaylist, viewModel.Name,
                viewModel.Shuffle, viewModel.Loop, 1, null, songs,
                fileMediaSources, null, DateTime.Now, DateTime.Now);

            await viewModel.PlaylistsRepo.InsertPlaylist(newPlaylist, null);
            await viewModel.LibraryRepo.SetCurrentPlaylistId(newPlaylist.Id);
        }
        else if(viewModel.SelectedPlaylist is { } selectedPlaylist)
        {
            string[] newAllPaths;
            if (viewModel.AppendSources)
            {
                Playlist playlist = await viewModel.PlaylistsRepo.GetPlaylist(selectedPlaylist.Id);
                IEnumerable<string> existingPaths = UpdateLibraryService.LoadAllFilePaths(playlist.FileMediaSources);
                newAllPaths = existingPaths.Concat(newPaths).Distinct().ToArray();
            }
            else
            {
                newAllPaths = newPaths.ToArray();
            }

            FileMediaSources fileMediaSources = FileMediaSourcesHelper.ExtractFileMediaSources(newAllPaths);
            await viewModel.PlaylistsRepo.SetFileMedisSources(selectedPlaylist.Id, fileMediaSources);
            await updateLibraryService.UpdateSourcePlaylist(selectedPlaylist.Id);
        }

        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}