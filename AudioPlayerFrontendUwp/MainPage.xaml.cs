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
using StdOttStandard.Converter.MultipleInputs;
using System.Threading.Tasks;

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
            DataContext = viewModel = (ViewModel)e.Parameter;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ServiceBuild build = viewModel.ServiceOpenBuild;

            if (build == null) await viewModel.ConnectAsync(Frame);
            else
            {
                switch (build.CompleteToken?.IsEnded)
                {
                    case BuildEndedType.Settings:
                        await NavigateToSettingsPage();
                        break;

                    case null:
                        Frame.Navigate(typeof(BuildOpenPage), viewModel.ServiceOpenBuild);
                        break;
                }
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
            }

            await NavigateToSettingsPage();
        }

        private async Task NavigateToSettingsPage()
        {
            await viewModel.CloseAsync();
            Frame.Navigate(typeof(SettingsPage), viewModel.Builder);
        }

        private object MicDoRemove_Convert(object sender, MultiplesInputsConvert2EventArgs args)
        {
            return !ReferenceEquals(args.Input0, args.Input1);
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

        private object MicCurrentSongIndex_ConvertRef(object sender, MultiplesInputsConvert4EventArgs args)
        {
            if (args.Input0 == null || args.ChangedValueIndex == 2) return args.Input0;

            IEnumerable<Song> allSongs = (IEnumerable<Song>)args.Input0;
            Song? currentSong = (Song?)args.Input1;
            int index = (int)args.Input3;

            if (args.ChangedValueIndex == 3 && index != -1) args.Input2 = RequestSong.Get(allSongs.ElementAt(index));
            else if (!currentSong.HasValue) args.Input3 = -1;
            else args.Input3 = allSongs.IndexOf(currentSong.Value);

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
