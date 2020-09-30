using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using AudioPlayerBackend.Build;
using StdOttStandard.Converter.MultipleInputs;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using StdOttStandard.AsyncResult;

namespace AudioPlayerFrontend
{
    public sealed partial class MainPage : Page
    {
        private ViewModel viewModel;
        private readonly ObservableCollection<IPlaylist> allPlaylists;

        public MainPage()
        {
            allPlaylists = new ObservableCollection<IPlaylist>();

            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = viewModel = (ViewModel)e.Parameter;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (viewModel.Service.ServiceOpenBuild?.CompleteToken?.IsEnded == BuildEndedType.Settings)
            {
                await NavigateToSettingsPage();
            }
        }

        private object MicPlaylists_Convert(object sender, MultiplesInputsConvert4EventArgs args)
        {
            IPlaylist[] newAllPlaylists = ((IEnumerable<ISourcePlaylist>)args.Input0).ToNotNull()
                .Concat(((IEnumerable<IPlaylist>)args.Input1).ToNotNull()).ToArray();

            for (int i = allPlaylists.Count - 1; i >= 0; i--)
            {
                if (!newAllPlaylists.Contains(allPlaylists[i])) allPlaylists.RemoveAt(i);
            }

            foreach ((int newIndex, IPlaylist playlist) in newAllPlaylists.WithIndex())
            {
                int oldIndex = allPlaylists.IndexOf(playlist);
                if (oldIndex == -1) allPlaylists.Insert(newIndex, playlist);
                else if (oldIndex != newIndex) allPlaylists.Move(oldIndex, newIndex);
            }

            if (args.ChangedValueIndex == 3 && args.Input3 != null) args.Input2 = args.Input3;
            else args.Input3 = args.Input2;

            return allPlaylists;
        }

        private object MicViewPlaylists_Convert(object sender, MultiplesInputsConvert2EventArgs args)
        {
            return true.Equals(args.Input0) || args.Input1 == null;
        }

        private void IbnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.Service.Audio != null) Frame.Navigate(typeof(SearchPage), viewModel.Service);
        }

        private void LbxSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Scroll();
        }

        private void Scroll()
        {
            if (lbxSongs.SelectedItem != null) lbxSongs.ScrollIntoView(lbxSongs.SelectedItem);
            else if (lbxSongs.Items.Count > 0) lbxSongs.ScrollIntoView(lbxSongs.Items[0]);
        }

        /// <summary>
        /// Show remove IconButton on every Song (true) or not (false).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">Input0 = SourcePlaylist, Input1 = CurrentPlaylist</param>
        /// <returns>Show remove IconButton on every Song (true) or not (false).</returns>
        private object MicDoRemove_Convert(object sender, MultiplesInputsConvert2EventArgs args)
        {
            return !(args.Input1 is ISourcePlaylistBase);
        }

        /// <summary>
        /// Handles user input and programmatic changes for viewed songs and selected song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">Input0: CurrentPlaylist.AllSongs, Input1: CurrentPlaylist.CurrentSong, Input2: CurrentPlaylist.WannaSong, Input3: lbxSongs.SelectedIndex</param>
        /// <returns>The list of Songs to view on UI.</returns>
        private object MicCurrentSongIndex_ConvertRef(object sender, MultiplesInputsConvert4EventArgs args)
        {
            if (args.Input0 == null)
            {
                args.Input3 = -1;
                return null;
            }
            if (args.ChangedValueIndex == 2) return args.Input0;

            IEnumerable<Song> allSongs = (IEnumerable<Song>)args.Input0;
            Song? currentSong = (Song?)args.Input1;
            int index = (int)args.Input3;

            if (args.ChangedValueIndex == 3 && index != -1) args.Input2 = RequestSong.Start(allSongs.ElementAt(index));
            else if (!currentSong.HasValue) args.Input3 = -1;
            else args.Input3 = allSongs.IndexOf(currentSong.Value);

            return allSongs;
        }

        private void IbnRemove_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;
            IAudioService service = viewModel.Service.Audio;

            if (service.CurrentPlaylist.Songs.All(s => s == song))
            {
                service.Playlists.Remove(service.CurrentPlaylist);
                /// TODO: Set correct playlist
            }
            else service.CurrentPlaylist.Songs = service.CurrentPlaylist.Songs.Where(s => s != song).ToArray();
        }

        private void IbnLoopType_Click(object sender, RoutedEventArgs e)
        {
            switch (viewModel.Service.Audio?.CurrentPlaylist.Loop)
            {
                case LoopType.Next:
                    viewModel.Service.Audio.CurrentPlaylist.Loop = LoopType.Stop;
                    break;

                case LoopType.Stop:
                    viewModel.Service.Audio.CurrentPlaylist.Loop = LoopType.CurrentPlaylist;
                    break;

                case LoopType.CurrentPlaylist:
                    viewModel.Service.Audio.CurrentPlaylist.Loop = LoopType.CurrentSong;
                    break;

                case LoopType.CurrentSong:
                    viewModel.Service.Audio.CurrentPlaylist.Loop = LoopType.StopCurrentSong;
                    break;

                case LoopType.StopCurrentSong:
                    viewModel.Service.Audio.CurrentPlaylist.Loop = LoopType.Next;
                    break;
            }
        }

        private void LbxPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            atbPlaylists.IsChecked = false;
        }

        private void SplCurrentSong_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Scroll();
        }

        private async void AbbUpdatePlaylistsAndSongs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.IsUpdatingPlaylists = true;
                await UpdateHelper.Update(viewModel.Service.Audio);
                Settings.Current.LastUpdatedData = DateTime.Now;
            }
            finally
            {
                viewModel.IsUpdatingPlaylists = false;
            }
        }

        private async void AbbUReloadPlaylistsAndSongs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.IsUpdatingPlaylists = true;
                await UpdateHelper.Reload(viewModel.Service.Audio);
                Settings.Current.LastUpdatedData = DateTime.Now;
            }
            finally
            {
                viewModel.IsUpdatingPlaylists = false;
            }
        }

        private void AudioPositionSlider_UserPositionChanged(object sender, TimeSpan e)
        {
            IPlaylist playlist = viewModel.Service.Audio?.CurrentPlaylist;
            if (playlist != null) playlist.WannaSong = RequestSong.Get(playlist.CurrentSong, e, playlist.Duration);
        }

        private async void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToSettingsPage();
        }

        private void AbbPrevious_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Service.Audio?.SetPreviousSong();
        }

        private void AbbPlayPause_Click(object sender, RoutedEventArgs e)
        {
            IAudioService service = viewModel.Service.Audio;

            if (service == null) return;

            service.PlayState = service.PlayState == PlaybackState.Playing
                ? PlaybackState.Paused
                : PlaybackState.Playing;
        }

        private void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Service.Audio?.SetNextSong();
        }

        private async void AbbSettings_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.Service.Audio != null)
            {
                viewModel.Service.Builder.WithService(viewModel.Service.Audio);
            }

            await NavigateToSettingsPage();
        }

        private async Task NavigateToSettingsPage()
        {
            AsyncResultS<ServiceBuilder> result = new AsyncResultS<ServiceBuilder>(viewModel.Service.Builder.Clone());
            Frame.Navigate(typeof(SettingsPage), result);

            ServiceBuilder newBuilder = await result.Task;

            if (newBuilder == null) return;

            viewModel.Service.Builder = newBuilder;

            await viewModel.Service.CloseAsync();
            await viewModel.Service.ConnectAsync(true);
        }

        private async void AbbDebug_Click(object sender, RoutedEventArgs e)
        {
            await new MessageDialog(App.CreateTime.ToString(CultureInfo.InvariantCulture), "CreateTime").ShowAsync();

            string exceptionText = Settings.Current.UnhandledExceptionText ?? "<null>";
            DateTime time = Settings.Current.UnhandledExceptionTime;

            await new MessageDialog(exceptionText, time.ToString()).ShowAsync();

            string message = $"Communicator: {viewModel.Service?.Communicator?.Name}\r\n" +
                $"State: {viewModel.Service?.Communicator?.IsOpen}\r\n" +
                $"Back: {AudioPlayerFrontend.Background.BackgroundTaskHandler.Current?.IsRunning}";
            await new MessageDialog(message).ShowAsync();
        }
    }
}
