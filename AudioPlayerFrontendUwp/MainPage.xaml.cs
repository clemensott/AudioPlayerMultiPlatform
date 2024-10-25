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
using StdOttStandard.TaskCompletionSources;
using System.Collections.Specialized;
using Windows.UI.Xaml.Controls.Primitives;
using StdOttUwp;
using AudioPlayerBackend;
using StdOttUwp.Converters;
using AudioPlayerBackend.FileSystem;
using AudioPlayerFrontend.Extensions;
using AudioPlayerBackend.ViewModels;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;
using StdOttStandard;

namespace AudioPlayerFrontend
{
    public sealed partial class MainPage : Page
    {
        private AudioServicesHandler audioServicesHandler;
        private ILibraryViewModel viewModel;
        private IUpdateLibraryService updateLibraryService;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            audioServicesHandler = (AudioServicesHandler)e.Parameter;
            audioServicesHandler.AudioServicesChanged += AudioServicesHandler_AudioServicesChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            audioServicesHandler.AudioServicesChanged -= AudioServicesHandler_AudioServicesChanged;
        }

        private void AudioServicesHandler_AudioServicesChanged(object sender, AudioServicesChangedEventArgs e)
        {
            DataContext = viewModel = e.NewServices.GetViewModel();
        }

        private void SetAudioServices(AudioServices audioServices)
        {
            DataContext = viewModel = audioServices?.GetViewModel();
            updateLibraryService = audioServices?.GetUpdateLibraryService();
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
            PlaylistInfo playlist = (PlaylistInfo)args.Input1;
            return !playlist.Type.HasFlag(PlaylistType.SourcePlaylist);
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

        private void IbnLoopType_Click(object sender, RoutedEventArgs e)
        {
            switch (viewModel?.CurrentPlaylist.Loop)
            {
                case LoopType.Next:
                    viewModel.CurrentPlaylist.Loop = LoopType.Stop;
                    break;

                case LoopType.Stop:
                    viewModel.CurrentPlaylist.Loop = LoopType.CurrentPlaylist;
                    break;

                case LoopType.CurrentPlaylist:
                    viewModel.CurrentPlaylist.Loop = LoopType.CurrentSong;
                    break;

                case LoopType.CurrentSong:
                    viewModel.CurrentPlaylist.Loop = LoopType.StopCurrentSong;
                    break;

                case LoopType.StopCurrentSong:
                    viewModel.CurrentPlaylist.Loop = LoopType.Next;
                    break;
            }
        }

        private void IbnOrderType_Click(object sender, RoutedEventArgs e)
        {
            switch (viewModel?.CurrentPlaylist.Shuffle)
            {
                case OrderType.ByTitleAndArtist:
                    viewModel.CurrentPlaylist.Shuffle = OrderType.ByPath;
                    break;

                case OrderType.ByPath:
                    viewModel.CurrentPlaylist.Shuffle = OrderType.Custom;
                    break;

                case OrderType.Custom:
                    viewModel.CurrentPlaylist.Shuffle = OrderType.ByTitleAndArtist;
                    break;
            }
        }

        private void IbnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null) Frame.NavigateToSearchPage(viewModel.SongSearch);
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
            PlaylistInfo playlist = (PlaylistInfo)args.Input0;
            return playlist.Type.HasFlag(PlaylistType.SourcePlaylist) && false.Equals(args.Input1);
        }

        private async void MfiUpdateSongs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //viewModel.IsUpdatingPlaylists = true;
                PlaylistInfo playlist = UwpUtils.GetDataContext<PlaylistInfo>(sender);
                await updateLibraryService.UpdateSourcePlaylist(playlist.Id);
            }
            catch (Exception exc)
            {
                await DialogUtils.ShowSafeAsync(exc.Message, "Update songs error");
            }
            finally
            {
                //viewModel.IsUpdatingPlaylists = false;
            }
        }

        private async void MfiReloadSongs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //viewModel.IsUpdatingPlaylists = true;
                PlaylistInfo playlist = UwpUtils.GetDataContext<PlaylistInfo>(sender);
                await updateLibraryService.ReloadSourcePlaylist(playlist.Id);
            }
            catch (Exception exc)
            {
                await DialogUtils.ShowSafeAsync(exc.Message, "Reload songs error");
            }
            finally
            {
                //viewModel.IsUpdatingPlaylists = false;
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

        private async void AbbUpdatePlaylistsAndSongs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //viewModel.IsUpdatingPlaylists = true;
                await updateLibraryService.UpdateLibrary();
            }
            finally
            {
                //viewModel.IsUpdatingPlaylists = false;
            }
        }

        private async void AbbUReloadPlaylistsAndSongs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //viewModel.IsUpdatingPlaylists = true;
                await updateLibraryService.ReloadLibrary();
            }
            finally
            {
                //viewModel.IsUpdatingPlaylists = false;
            }
        }

        private async void AudioPositionSlider_UserPositionChanged(object sender, TimeSpan e)
        {
            IPlaylistViewModel playlist = viewModel.CurrentPlaylist;
            RequestSong? requestSong = RequestSong.Get(playlist.CurrentSong, e, playlist.Duration);
            await viewModel.CurrentPlaylist.SendRequestSong(requestSong);
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

            string message = $"Back: {AudioPlayerFrontend.Background.BackgroundTaskHandler.Current?.IsRunning}";
            await new MessageDialog(message, "States").ShowAsync();

            await new MessageDialog(Logs.Get(), "Logs").ShowAsync();
        }
    }
}
