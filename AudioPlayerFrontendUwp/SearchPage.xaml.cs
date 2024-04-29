using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls.Primitives;
using StdOttStandard.Converter.MultipleInputs;

namespace AudioPlayerFrontend
{
    public sealed partial class SearchPage : Page
    {
        private IAudioService service;

        public SearchPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = service = (IAudioService)e.Parameter;

            base.OnNavigatedTo(e);
        }

        private void IbnPlay_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;

            service.AddSongsToFirstPlaylist(new Song[] { song }, true);
        }

        private void IbnAdd_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;

            service.AddSongsToFirstPlaylist(new Song[] { song });
        }

        private void IbnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<Song> songs = (IEnumerable<Song>)micSongs.Output;

            service.AddSongsToFirstPlaylist(songs);
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (service.Playlists.Count > 0)
            {
                service.Playlists[0].Songs = new Song[0];
            }
        }

        private void IbnBack_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private object MicSongs_Convert(object sender, MultiplesInputsConvert2EventArgs args)
        {
            if (service == null) return null;

            IEnumerable<Song> viewSongs = service.IsSearching ?
                service.SearchSongs : service.AllSongs;

            if (service.CurrentPlaylist is null || service.CurrentPlaylist is ISourcePlaylist) return viewSongs;

            return viewSongs.Except(service.CurrentPlaylist.Songs);
        }

        private object SicPlaylist_Convert(object sender, SingleInputsConvertEventArgs args)
        {
            return ((IEnumerable<IPlaylistBase>)args.Input)?.FirstOrDefault();
        }

        private object SicSongsCount_Convert(object sender, SingleInputsConvertEventArgs args)
        {
            return ((IList<Song>)args.Input)?.Count ?? -1;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Selector selector = (Selector)sender;
            selector.SelectedItem = null;
        }
    }
}
