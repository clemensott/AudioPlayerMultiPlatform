using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using AudioPlayerFrontend.Join;
using Windows.UI.Xaml.Controls.Primitives;
using StdOttStandard.Converter.MultipleInputs;

namespace AudioPlayerFrontend
{
    public sealed partial class SearchPage : Page
    {
        private ServiceHandler service;

        public SearchPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = service = (ServiceHandler)e.Parameter;

            base.OnNavigatedTo(e);
        }

        private void IbnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (service.Audio == null) return;

            Song song = (Song)((FrameworkElement)sender).DataContext;

            service.Audio.AddSongsToFirstPlaylist(new Song[] { song }, true, AudioServiceHelper.Current);
        }

        private void IbnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (service.Audio == null) return;

            Song song = (Song)((FrameworkElement)sender).DataContext;

            service.Audio.AddSongsToFirstPlaylist(new Song[] { song }, AudioServiceHelper.Current);
        }

        private void IbnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (service.Audio == null) return;

            IEnumerable<Song> songs = (IEnumerable<Song>)micSongs.Output;

            service.Audio.AddSongsToFirstPlaylist(songs, AudioServiceHelper.Current);
        }
        
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (service.Audio.Playlists.Length > 0)
            {
                service.Audio.Playlists[0].Songs = new Song[0];
            }
        }

        private void IbnBack_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private object MicSongs_Convert(object sender, MultiplesInputsConvert2EventArgs args)
        {
            IAudioService audio = service?.Audio;

            if (audio == null) return null;

            IEnumerable<Song> viewSongs = audio.SourcePlaylist.IsSearching ?
                audio.SourcePlaylist.SearchSongs : audio.SourcePlaylist.AllSongs;

            if (audio.CurrentPlaylist == audio.SourcePlaylist) return viewSongs;

            return viewSongs.Except(audio.CurrentPlaylist.Songs);
        }

        private object SicPlaylist_Convert(object sender, SingleInputsConvertEventArgs args)
        {
            return ((IEnumerable<IPlaylistBase>)args.Input)?.FirstOrDefault();
        }

        private object SicSongsCount_Convert(object sender, SingleInputsConvertEventArgs args)
        {
            return ((Array)args.Input)?.Length ?? -1;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Selector selector = (Selector)sender;
            selector.SelectedItem = null;
        }
    }
}
