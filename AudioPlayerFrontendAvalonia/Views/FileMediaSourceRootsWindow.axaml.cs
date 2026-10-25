using System.Collections.Generic;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AudioPlayerFrontendAvalonia.Views;

public partial class FileMediaSourceRootsWindow : Window
{
    public FileMediaSourceRootsWindow()
    {
        InitializeComponent();
    }

    public FileMediaSourceRootsWindow(IEnumerable<FileMediaSourceRootWithPlaylists> roots, IPlaylistsRepo playlistsRepo) : this()
    {
        foreach (FileMediaSourceRootWithPlaylists root in roots)
        {
            gidContent.Children.Add(CreateRootControl(root, playlistsRepo));
        }
    }

    private Control CreateRootControl(FileMediaSourceRootWithPlaylists root, IPlaylistsRepo playlistsRepo)
    {
        Grid container = new Grid()
        {
            Margin = new Thickness(3),
            ColumnDefinitions = new ColumnDefinitions("*,auto"),
            RowDefinitions = new RowDefinitions("auto,auto"),
        };

        TextBlock tblName = new TextBlock()
        {
            Text = root.Name,
            Margin = new Thickness(3),
            FontWeight = FontWeight.Bold,
        };
        container.Children.Add(tblName);

        StackPanel splPlaylists = new StackPanel()
        {
            Margin = new Thickness(3),
        };
        splPlaylists.SetValue(Grid.RowProperty, 1);
        splPlaylists.SetValue(Grid.RowSpanProperty, 2);
        
        foreach (PlaylistInfo playlist in root.Playlists)
        {
            TextBlock tblPlaylist = new TextBlock()
            {
                Text = $"- {playlist.Name} (Songs: {playlist.SongsCount})",
                Margin = new Thickness(2),
            };
            splPlaylists.Children.Add(tblPlaylist);
        }
        
        container.Children.Add(splPlaylists);

        Button btnDelete = new Button()
        {
            Content = "x",
            Margin = new Thickness(3),
        };
        btnDelete.SetValue(Grid.ColumnProperty, 3);
        btnDelete.Click += async (_, _) =>
        {
            foreach (PlaylistInfo playlist in root.Playlists)
            {
                await playlistsRepo.RemovePlaylist(playlist.Id);
            }
        };
        container.Children.Add(btnDelete);

        return container;
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}