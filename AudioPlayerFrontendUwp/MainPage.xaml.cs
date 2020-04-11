using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using AudioPlayerBackend.Build;

namespace AudioPlayerFrontend
{
    public sealed partial class MainPage : Page
    {
        private ViewModel viewModel;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ViewModel)
            {
                DataContext = viewModel = e.Parameter as ViewModel;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ServiceBuild build = viewModel.ServiceOpenBuild;

            if (build == null)
            {
                build = viewModel.Build();
                Frame.Navigate(typeof(BuildOpenPage), build);
                return;
            }

            switch (build.CompleteToken?.IsEnded)
            {
                case BuildEndedType.Settings:
                    NavigateToSettingsPage();
                    break;

                case null:
                    Frame.Navigate(typeof(BuildOpenPage), viewModel.ServiceOpenBuild);
                    break;
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.SourcePlaylist.Reload();
        }

        private void IbnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.AudioService != null) Frame.Navigate(typeof(SearchPage), viewModel);
        }

        private void LbxSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Scroll();
        }

        private void AudioPositionSlider_UserPositionChanged(object sender, TimeSpan e)
        {
            IPlaylist playlist = viewModel.AudioService?.CurrentPlaylist;
            if (playlist != null) playlist.WannaSong = RequestSong.Get(playlist.CurrentSong, e);
        }

        private void Scroll()
        {
            if (lbxSongs.SelectedItem != null) lbxSongs.ScrollIntoView(lbxSongs.SelectedItem);
            else if (lbxSongs.Items.Count > 0) lbxSongs.ScrollIntoView(lbxSongs.Items[0]);
        }

        private void AbbPrevious_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.SetPreviousSong();
        }

        private void AbbPlayPause_Click(object sender, RoutedEventArgs e)
        {
            IAudioService service = viewModel.AudioService;

            if (service == null) return;

            service.PlayState = service.PlayState == PlaybackState.Playing ? PlaybackState.Paused : PlaybackState.Playing;
        }

        private void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.SetNextSong();
        }

        private async void AbbSettings_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.AudioService != null)
            {
                viewModel.Builder.WithService(viewModel.AudioService);

                try
                {
                    if (viewModel.Communicator != null) await viewModel.Communicator.CloseAsync();
                }
                catch { }
            }

            NavigateToSettingsPage();
        }

        private void NavigateToSettingsPage()
        {
            viewModel.ServiceOpenBuild = null;
            Frame.Navigate(typeof(SettingsPage), viewModel.Builder);
        }


        private object MicDoRemove_Convert(object input0, object input1, int changedIndex)
        {
            return !ReferenceEquals(input0, input1);
        }

        private void IbnRemove_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;
            IAudioService service = viewModel.AudioService;

            if (service.CurrentPlaylist.Songs.All(s => s == song))
            {
                service.Playlists.Remove(service.CurrentPlaylist);
            }
            else service.CurrentPlaylist.Songs = service.CurrentPlaylist.Songs.Where(s => s != song).ToArray();
        }

        private void IbnLoopType_Click(object sender, RoutedEventArgs e)
        {
            switch (viewModel.AudioService?.CurrentPlaylist.Loop)
            {
                case LoopType.Next:
                    viewModel.AudioService.CurrentPlaylist.Loop = LoopType.Stop;
                    break;

                case LoopType.Stop:
                    viewModel.AudioService.CurrentPlaylist.Loop = LoopType.CurrentPlaylist;
                    break;

                case LoopType.CurrentPlaylist:
                    viewModel.AudioService.CurrentPlaylist.Loop = LoopType.CurrentSong;
                    break;

                case LoopType.CurrentSong:
                    viewModel.AudioService.CurrentPlaylist.Loop = LoopType.StopCurrentSong;
                    break;

                case LoopType.StopCurrentSong:
                    viewModel.AudioService.CurrentPlaylist.Loop = LoopType.Next;
                    break;
            }
        }

        private void SplCurrentSong_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Scroll();
        }

        private object MicCurrentSongIndex_ConvertRef(ref object rawAllSongs,
            ref object rawCurrentSong, ref object rawWannaSong, ref object rawIndex, int changedInput)
        {
            if (rawAllSongs == null || changedInput == 2) return rawAllSongs;

            IEnumerable<Song> allSongs = (IEnumerable<Song>)rawAllSongs;
            Song? currentSong = (Song?)rawCurrentSong;
            int index = (int)rawIndex;

            if (changedInput == 3 && index != -1) rawWannaSong = RequestSong.Get(allSongs.ElementAt(index));
            else if (!currentSong.HasValue) rawIndex = -1;
            else rawIndex = allSongs.IndexOf(currentSong.Value);

            return allSongs;
        }

        private async void AbbDebug_Click(object sender, RoutedEventArgs e)
        {
            await new MessageDialog("CreateTime: " + App.CreateTime).ShowAsync();

            string exceptionText;

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("Exception.txt");

                exceptionText = await FileIO.ReadTextAsync(file);
            }
            catch (Exception exc)
            {
                exceptionText = exc.ToString();
            }

            await new MessageDialog(exceptionText).ShowAsync();
        }
    }
}
