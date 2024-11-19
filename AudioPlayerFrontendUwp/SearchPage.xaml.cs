using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls.Primitives;
using StdOttStandard.Converter.MultipleInputs;
using AudioPlayerBackend.ViewModels;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using AudioPlayerBackend.Build;
using Windows.UI.Core;

namespace AudioPlayerFrontend
{
    public sealed partial class SearchPage : Page
    {
        private AudioServicesHandler audioServicesHandler;
        private ISongSearchViewModel viewModel;

        public SearchPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            audioServicesHandler = (AudioServicesHandler)e.Parameter;
            audioServicesHandler.AddAudioServicesChangedListener(AudioServicesHandler_AudioServicesChanged);
        }

        private async void AudioServicesHandler_AudioServicesChanged(object sender, AudioServicesChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (viewModel != null) await viewModel.Stop();
                DataContext = viewModel = e.NewServices?.GetViewModel().SongSearch;
                if (viewModel != null) await viewModel.Start();
            });
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (viewModel != null) await viewModel.Stop();
        }

        private async void IbnPlay_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;

            await viewModel.AddSongsToSearchPlaylist(new Song[] { song }, SearchPlaylistAddType.FirstInPlaylist);
        }

        private async void IbnNext_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;

            await viewModel.AddSongsToSearchPlaylist(new Song[] { song }, SearchPlaylistAddType.NextInPlaylist);
        }

        private async void IbnAdd_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;

            await viewModel.AddSongsToSearchPlaylist(new Song[] { song }, SearchPlaylistAddType.LastInPlaylist);
        }

        private async void IbnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<Song> songs = viewModel.SearchSongs;

            await viewModel.AddSongsToSearchPlaylist(songs, SearchPlaylistAddType.LastInPlaylist);
        }

        private async void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.SearchPlaylist.ClearSongs();
        }

        private void IbnBack_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private object SicSongsCount_Convert(object sender, SingleInputsConvertEventArgs args)
        {
            return ((ICollection<Song>)args.Input)?.Count ?? -1;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Selector selector = (Selector)sender;
            selector.SelectedItem = null;
        }
    }
}
