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
using StdOttStandard.TaskCompletionSources;
using Windows.UI.Xaml.Controls.Primitives;
using StdOttUwp;
using AudioPlayerBackend;
using AudioPlayerBackend.FileSystem;
using AudioPlayerFrontend.Extensions;
using AudioPlayerBackend.ViewModels;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using StdOttStandard;
using AudioPlayerFrontend.Controls;
using Windows.UI.Core;
using AudioPlayerBackendUwpLib;

namespace AudioPlayerFrontend
{
    public sealed partial class MainPage : Page
    {
        private AudioServicesHandler audioServicesHandler;
        private ILibraryViewModel viewModel;
        private IUpdateLibraryService updateLibraryService;

        public MainPage()
        {
            Logs.Log("MainPage1");
            this.InitializeComponent();
            Logs.Log("MainPage2");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Logs.Log("MainPage.OnNavigatedTo1");
            audioServicesHandler = (AudioServicesHandler)e.Parameter;
            audioServicesHandler.AddAudioServicesChangedListener(AudioServicesHandler_AudioServicesChanged);
            Logs.Log("MainPage.OnNavigatedTo2");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            audioServicesHandler.AudioServicesChanged -= AudioServicesHandler_AudioServicesChanged;
        }

        private async void AudioServicesHandler_AudioServicesChanged(object sender, AudioServicesChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetAudioServices(e.NewServices));
        }

        private void SetAudioServices(AudioServices audioServices)
        {
            Logs.Log("MainPage.SetAudioServices1");
            DataContext = viewModel = audioServices?.GetViewModel();
            updateLibraryService = audioServices?.GetUpdateLibraryService();
            Logs.Log("MainPage.SetAudioServices2");
        }

        private object MicViewPlaylists_Convert(object sender, MultiplesInputsConvert2EventArgs args)
        {
            return true.Equals(args.Input0) || args.Input1 == null;
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
            PlaylistType? playlistType = (PlaylistType?)args.Input1;
            return playlistType?.HasFlag(PlaylistType.SourcePlaylist) == false;
        }

        /// <summary>
        /// Handles user input and programmatic changes for viewed songs and selected song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">Input0: CurrentPlaylist.AllSongs, Input1: CurrentPlaylist.CurrentSongRequest, Input2: lbxSongs.SelectedIndex</param>
        /// <returns>The list of Songs to view on UI.</returns>
        private object MicCurrentSongIndex_ConvertRef(object sender, MultiplesInputsConvert3EventArgs args)
        {
            if (args.Input0 == null)
            {
                args.Input2 = -1;
                return null;
            }

            IEnumerable<Song> allSongs = (IEnumerable<Song>)args.Input0;
            SongRequest? songRequest = (SongRequest?)args.Input1;
            int index = (int)args.Input2;
            Guid? currentSongId = songRequest?.Id;

            if (args.ChangedValueIndex == 2 && index != -1) args.Input1 = SongRequest.Start(allSongs.ElementAt(index).Id);
            else if (!currentSongId.HasValue) args.Input2 = -1;
            else args.Input2 = allSongs.IndexOf(s => s.Id == currentSongId);

            return allSongs;
        }

        private async void IbnRemove_Click(object sender, RoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;

            if (!viewModel.CurrentPlaylist.Songs.All(s => s == song))
            {
                await viewModel.CurrentPlaylist.RemoveSong(song.Id);
            }
            else if (viewModel.CurrentPlaylist.Id.TryHasValue(out Guid currentPlaylistId))
            {
                await viewModel.RemovePlaylist(currentPlaylistId);
            }
        }

        private async void IbnLoopType_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<ListBoxDialogItem<LoopType?>> options = new ListBoxDialogItem<LoopType?>[]
            {
                new ListBoxDialogItem<LoopType?>(LoopType.CurrentPlaylist, "Repeat Current Playlist"),
                new ListBoxDialogItem<LoopType?>(LoopType.Stop, "Stop Playback"),
                new ListBoxDialogItem<LoopType?>(LoopType.Next, "Play Next Playlist"),
                new ListBoxDialogItem<LoopType?>(LoopType.CurrentSong, "Repeat Current Song"),
                new ListBoxDialogItem<LoopType?>(LoopType.StopCurrentSong, "Stop Playback & Keep Current Song"),
            };

            LoopType? newValue = await ListBoxDialog<LoopType?>
                .Start(options, viewModel.CurrentPlaylist.Loop, "Loop Type");

            if (newValue.HasValue) viewModel.CurrentPlaylist.Loop = newValue.Value;
        }

        private async void IbnOrderType_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<ListBoxDialogItem<OrderType?>> options = new ListBoxDialogItem<OrderType?>[]
            {
                new ListBoxDialogItem<OrderType?>(OrderType.ByTitleAndArtist, "By Title And Artist"),
                new ListBoxDialogItem<OrderType?>(OrderType.ByPath, "By Path"),
                new ListBoxDialogItem<OrderType?>(OrderType.Custom, "Shuffle"),
            };

            OrderType? newValue = await ListBoxDialog<OrderType?>
                .Start(options, viewModel.CurrentPlaylist.Shuffle, "Order Type");

            if (newValue.HasValue) viewModel.CurrentPlaylist.Shuffle = newValue.Value;
        }

        private void IbnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null) Frame.NavigateToSearchPage(audioServicesHandler);
        }

        private void LbxPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            atbPlaylists.IsChecked = false;
        }

        private void GidPlaylistItem_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void GidPlaylistItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private object MicPlaylistUpdateable_Convert(object sender, MultiplesInputsConvert2EventArgs args)
        {
            PlaylistType? playlistType = (PlaylistType?)args.Input0;
            return playlistType?.HasFlag(PlaylistType.SourcePlaylist) == true && false.Equals(args.Input1);
        }

        private async void MfiUpdateSongs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PlaylistInfo playlist = UwpUtils.GetDataContext<PlaylistInfo>(sender);
                await updateLibraryService.UpdateSourcePlaylist(playlist.Id);
            }
            catch (Exception exc)
            {
                await DialogUtils.ShowSafeAsync(exc.Message, "Update songs error");
            }
        }

        private async void MfiReloadSongs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PlaylistInfo playlist = UwpUtils.GetDataContext<PlaylistInfo>(sender);
                await updateLibraryService.ReloadSourcePlaylist(playlist.Id);
            }
            catch (Exception exc)
            {
                await DialogUtils.ShowSafeAsync(exc.Message, "Reload songs error");
            }
        }

        private async void MfiRemixSongs_Click(object sender, RoutedEventArgs e)
        {
            PlaylistInfo playlist = UwpUtils.GetDataContext<PlaylistInfo>(sender);
            await viewModel.RemixSongs(playlist.Id);
        }

        private void MfiRemovePlaylist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistInfo playlist = UwpUtils.GetDataContext<PlaylistInfo>(sender);
            viewModel.RemovePlaylist(playlist.Id);
        }

        private void SplCurrentSong_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Scroll();
        }

        private async void SplPlaybackRate_Tapped(object sender, TappedRoutedEventArgs e)
        {
            double[] playbackRates = new double[] { 0.5, 0.75, 0.9, 1, 1.15, 1.3, 1.5, 1.75, 2, 2.25, 2.5 };
            IEnumerable<ListBoxDialogItem<double>> options = playbackRates
                .Select(rate => new ListBoxDialogItem<double>(rate, $"{rate,2}x"));

            double newValue = await ListBoxDialog<double>
                .Start(options, viewModel.CurrentPlaylist.PlaybackRate, "Playback Rate");

            if (newValue > 0) viewModel.CurrentPlaylist.PlaybackRate = newValue;
        }

        private async void AbbUpdatePlaylistsAndSongs_Click(object sender, RoutedEventArgs e)
        {
            await updateLibraryService.UpdateLibrary();
        }

        private async void AbbUReloadPlaylistsAndSongs_Click(object sender, RoutedEventArgs e)
        {
            await updateLibraryService.ReloadLibrary();
        }

        private async void AudioPositionSlider_UserPositionChanged(object sender, TimeSpan e)
        {
            IPlaylistViewModel playlist = viewModel.CurrentPlaylist;
            SongRequest? songRequest = SongRequest.Get(playlist.CurrentSongRequest?.Id, e, playlist.CurrentSongRequest?.Duration);
            await viewModel.CurrentPlaylist.SetCurrentSongRequest(songRequest);
        }

        private async void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToSettingsPage();
        }

        private async void AbbPrevious_Click(object sender, RoutedEventArgs e)
        {
            await viewModel?.CurrentPlaylist.SetPreviousSong();
        }

        private void AbbPlayPause_Click(object sender, RoutedEventArgs e)
        {
            viewModel?.SetTogglePlayState();
        }

        private async void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            await viewModel?.CurrentPlaylist.SetNextSong();
        }

        private async void AbbSettings_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToSettingsPage();
        }

        private async Task NavigateToSettingsPage()
        {
            TaskCompletionSourceS<AudioServicesBuildConfig> result =
                new TaskCompletionSourceS<AudioServicesBuildConfig>(audioServicesHandler.Config.Clone());
            Frame.NavigateToSettingsPage(result);

            AudioServicesBuildConfig newConfig = await result.Task;

            if (newConfig != null) audioServicesHandler.Start(newConfig);
        }

        private async void AbbDebug_Click(object sender, RoutedEventArgs e)
        {
            await new MessageDialog(Settings.Current.SuspendTime.ToString(CultureInfo.InvariantCulture), "SuspendTime").ShowAsync();

            string exceptionText = Settings.Current.UnhandledExceptionText ?? "<null>";
            DateTime time = Settings.Current.UnhandledExceptionTime;

            await new MessageDialog(exceptionText, time.ToString()).ShowAsync();

            string message = $"Back: {AudioPlayerFrontend.Background.BackgroundTaskHandler.Current?.IsRunning}"
                + $"\nAudioHandler: {audioServicesHandler.IsStarted}"
                + $"\nConfig: {audioServicesHandler.Config != null}"
                + $"\nAudioServices: {audioServicesHandler.AudioServices != null}";
            await new MessageDialog(message, "States").ShowAsync();

            const int maxLength = 15000;
            string logs = await Logs.GetFile();
            if (logs.Length > maxLength) logs = "[...]" + logs.Substring(logs.Length - maxLength);
            bool keepLogs = await DialogUtils.ShowTwoOptionsAsync(logs, "Logs", "OK", "Clear");
            if (!keepLogs) await Logs.ClearAll();
        }
    }
}
