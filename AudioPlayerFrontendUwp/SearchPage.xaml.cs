using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using AudioPlayerFrontend.Join;

namespace AudioPlayerFrontend
{
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

            base.OnNavigatedTo(e);
        }

        private void SyiPlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (viewModel.AudioService == null) return;

            Song song = (Song)((FrameworkElement)sender).DataContext;

            viewModel.AudioService.AddSongsToFirstPlaylist(new Song[] { song }, true, AudioServiceHelper.Current);

            viewModel.AudioService.CurrentPlaylist.CurrentSong = song;
        }

        private void SyiAdd_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (viewModel.AudioService == null) return;

            Song song = (Song)((FrameworkElement)sender).DataContext;

            viewModel.AudioService.AddSongsToFirstPlaylist(new Song[] { song }, AudioServiceHelper.Current);
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.AudioService == null) return;

            IEnumerable<Song> songs = (IEnumerable<Song>)micSongs.Output;

            viewModel.AudioService.AddSongsToFirstPlaylist(songs, AudioServiceHelper.Current);
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((ListBox)sender).SelectedItem = null;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.AudioService.Playlists.Length > 0)
            {
                viewModel.AudioService.Playlists[0].Songs = new Song[0];
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

        private object MicSongs_Convert(object input0, object input1, int changedIndex)
        {
            IAudioService service = viewModel?.AudioService;

            if (service == null) return null;

            IEnumerable<Song> viewSongs = service.SourcePlaylist.IsSearching ?
                service.SourcePlaylist.SearchSongs : service.SourcePlaylist.AllSongs;

            if (service.CurrentPlaylist == service.SourcePlaylist) return viewSongs;

            return viewSongs.Except(service.CurrentPlaylist.Songs);
        }

        private object SicPlaylist_Convert(object input, int changedInput)
        {
            return ((IEnumerable<IPlaylistBase>)input)?.FirstOrDefault();
        }

        private object SicSongsCount_Convert(object input, int changedIndex)
        {
            return ((Array)input)?.Length ?? -1;
        }
    }
}
