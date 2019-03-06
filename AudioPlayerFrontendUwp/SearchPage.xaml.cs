using AudioPlayerBackend;
using StdOttStandard;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SearchPage : Page
    {
        private ViewModel viewModel;

        public SearchPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = viewModel = (ViewModel)e.Parameter;
            //playlist = viewModel.AudioService?.AdditionalPlaylists.FirstOrDefault();

            base.OnNavigatedTo(e);
        }

        private void SyiPlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;

            if (viewModel.AudioService == null) return;

            if (viewModel.AudioService.AdditionalPlaylists.Count > 0)
            {
                PlaylistViewModel playlist = viewModel.AudioService.AdditionalPlaylists[0];

                playlist.Songs = new Song[] { song }.Concat(playlist.Songs).Distinct().ToArray();
                playlist.CurrentSong = song;

                viewModel.AudioService.CurrentPlaylist = playlist;
            }
            else
            {
                IPlaylistExtended newPlaylist = new Playlist
                {
                    Loop = LoopType.Next,
                    IsAllShuffle = true,
                    Songs = new Song[] { song },
                    CurrentSong = song
                };

                viewModel.AudioService.Base.AdditionalPlaylists.Add(newPlaylist);
                viewModel.AudioService.Base.CurrentPlaylist = newPlaylist;
            }
        }

        private void SyiAdd_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;

            if (viewModel.AudioService == null) return;

            if (viewModel.AudioService.AdditionalPlaylists.Count > 0)
            {
                PlaylistViewModel playlist = viewModel.AudioService.AdditionalPlaylists[0];

                playlist.Songs = playlist.Songs.Concat(song).Distinct().ToArray();
                viewModel.AudioService.CurrentPlaylist = playlist;
            }
            else
            {
                IPlaylistExtended newPlaylist = new Playlist
                {
                    Loop = LoopType.Next,
                    IsAllShuffle = true,
                    Songs = new Song[] { song },
                    CurrentSong = song
                };

                viewModel.AudioService.Base.AdditionalPlaylists.Add(newPlaylist);
                viewModel.AudioService.Base.CurrentPlaylist = newPlaylist;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((ListBox)sender).SelectedItem = null;
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<Song> songs = (IEnumerable<Song>)micSongs.Output;

            if (viewModel.AudioService.AdditionalPlaylists.Count > 0)
            {
                PlaylistViewModel playlist = viewModel.AudioService.AdditionalPlaylists[0];

                playlist.Songs = playlist.Songs.Concat(songs).ToArray();
                viewModel.AudioService.CurrentPlaylist = playlist;
            }
            else
            {
                IPlaylistExtended newPlaylist = new Playlist
                {
                    Loop = LoopType.Next,
                    IsAllShuffle = true,
                    Songs = songs.Distinct().ToArray(),
                    CurrentSong = songs.Any() ? (Song?)songs.First() : null
                };

                viewModel.AudioService.Base.AdditionalPlaylists.Add(newPlaylist);
                viewModel.AudioService.Base.CurrentPlaylist = newPlaylist;
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private object MicSongs_Convert(object input0, object input1)
        {
            AudioViewModel audio = viewModel?.AudioService;

            if (audio == null) return null;
            if (audio.CurrentPlaylist == audio.FileBasePlaylist) return audio.FileBasePlaylist.ViewSongs;

            return audio.FileBasePlaylist.ViewSongs.Except(audio.CurrentPlaylist.Songs);
        }
    }
}
