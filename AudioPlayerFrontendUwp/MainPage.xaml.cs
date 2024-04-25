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
using StdOttStandard.TaskCompletionSources;
using System.Collections.Specialized;
using Windows.UI.Xaml.Controls.Primitives;
using StdOttUwp;
using AudioPlayerBackend;
using StdOttUwp.Converters;
using AudioPlayerBackend.FileSystem;

namespace AudioPlayerFrontend
{
    public sealed partial class MainPage : Page
    {
        private readonly IFileSystemService fileSystemService;
        private ServiceHandler serviceHandler;
        private ViewModel viewModel;
        private readonly ObservableCollection<IPlaylist> allPlaylists;

        public MainPage()
        {
            fileSystemService = AudioPlayerServiceProvider.Current.GetFileSystemService();
            allPlaylists = new ObservableCollection<IPlaylist>();

            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            serviceHandler = (ServiceHandler)e.Parameter;
            DataContext = viewModel = serviceHandler.ViewModel;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (serviceHandler.ServiceOpenBuild?.CompleteToken?.IsEnded == BuildEndedType.Settings)
            {
                await NavigateToSettingsPage();
            }
        }

        private object MicPlaylists_Convert(object sender, MultiplesInputsConvert4EventArgs args)
        {
            MultipleInputs4Converter converter = (MultipleInputs4Converter)sender;

            if (args.ChangedValueIndex == 0)
            {
                if (args.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnCollectionChanged;
                if (args.Input0 is INotifyCollectionChanged newList) newList.CollectionChanged += OnCollectionChanged;
            }
            else if (args.ChangedValueIndex == 1)
            {
                if (args.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnCollectionChanged;
                if (args.Input1 is INotifyCollectionChanged newList) newList.CollectionChanged += OnCollectionChanged;
            }

            UpdateAllPlaylists();

            if (args.ChangedValueIndex == 3 && args.Input3 != null) args.Input2 = args.Input3;
            else args.Input3 = args.Input2;

            return allPlaylists;

            void OnCollectionChanged(object s, NotifyCollectionChangedEventArgs e)
            {
                UpdateAllPlaylists();
            }

            void UpdateAllPlaylists()
            {
                IPlaylist[] newAllPlaylists = ((IEnumerable<ISourcePlaylist>)converter.Input0).ToNotNull()
                    .Concat(((IEnumerable<IPlaylist>)converter.Input1).ToNotNull()).ToArray();

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
            }
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
            IAudioService service = viewModel.Audio;
            IPlaylist playlist = service.CurrentPlaylist;

            if (playlist.Songs.All(s => s == song))
            {
                service.CurrentPlaylist = service.GetAllPlaylists().Where(p => p != playlist).Any() ?
                    service.GetAllPlaylists().Next(playlist).next : null;

                service.Playlists.Remove(playlist);
            }
            else playlist.Songs = playlist.Songs.Where(s => s != song).ToArray();
        }

        private void IbnLoopType_Click(object sender, RoutedEventArgs e)
        {
            switch (viewModel.Audio?.CurrentPlaylist.Loop)
            {
                case LoopType.Next:
                    viewModel.Audio.CurrentPlaylist.Loop = LoopType.Stop;
                    break;

                case LoopType.Stop:
                    viewModel.Audio.CurrentPlaylist.Loop = LoopType.CurrentPlaylist;
                    break;

                case LoopType.CurrentPlaylist:
                    viewModel.Audio.CurrentPlaylist.Loop = LoopType.CurrentSong;
                    break;

                case LoopType.CurrentSong:
                    viewModel.Audio.CurrentPlaylist.Loop = LoopType.StopCurrentSong;
                    break;

                case LoopType.StopCurrentSong:
                    viewModel.Audio.CurrentPlaylist.Loop = LoopType.Next;
                    break;
            }
        }

        private void IbnOrderType_Click(object sender, RoutedEventArgs e)
        {
            switch (viewModel.Audio?.CurrentPlaylist.Shuffle)
            {
                case OrderType.ByTitleAndArtist:
                    viewModel.Audio.CurrentPlaylist.Shuffle = OrderType.ByPath;
                    break;

                case OrderType.ByPath:
                    viewModel.Audio.CurrentPlaylist.Shuffle = OrderType.Custom;
                    break;

                case OrderType.Custom:
                    viewModel.Audio.CurrentPlaylist.Shuffle = OrderType.ByTitleAndArtist;
                    break;
            }
        }

        private void IbnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.Audio != null) Frame.Navigate(typeof(SearchPage), viewModel.Audio);
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
            return args.Input0 is ISourcePlaylist && false.Equals(args.Input1);
        }

        private async void MfiUpdateSongs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.IsUpdatingPlaylists = true;
                await fileSystemService.UpdateSourcePlaylist(UwpUtils.GetDataContext<ISourcePlaylist>(sender));
            }
            catch (Exception exc)
            {
                await DialogUtils.ShowSafeAsync(exc.Message, "Update songs error");
            }
            finally
            {
                viewModel.IsUpdatingPlaylists = false;
            }
        }

        private async void MfiReloadSongs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.IsUpdatingPlaylists = true;
                await fileSystemService.ReloadSourcePlaylist(UwpUtils.GetDataContext<ISourcePlaylist>(sender));
            }
            catch (Exception exc)
            {
                await DialogUtils.ShowSafeAsync(exc.Message, "Reload songs error");
            }
            finally
            {
                viewModel.IsUpdatingPlaylists = false;
            }
        }

        private void MfiRemixSongs_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = UwpUtils.GetDataContext<IPlaylist>(sender);
            playlist.Songs = playlist.Songs.Shuffle().ToArray();
        }

        private void MfiRemovePlaylist_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = UwpUtils.GetDataContext<IPlaylist>(sender);
            IAudioService service = viewModel.Audio;

            if (service.CurrentPlaylist == playlist)
            {
                service.CurrentPlaylist = service.GetAllPlaylists().Where(p => p != playlist).Any() ?
                    service.GetAllPlaylists().Next(playlist).next : null;
            }

            if (playlist is ISourcePlaylist) service.SourcePlaylists.Remove((ISourcePlaylist)playlist);
            else service.Playlists.Remove(playlist);
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
                await UpdateHelper.Update(viewModel.Audio);
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
                await UpdateHelper.Reload(viewModel.Audio);
                Settings.Current.LastUpdatedData = DateTime.Now;
            }
            finally
            {
                viewModel.IsUpdatingPlaylists = false;
            }
        }

        private void AudioPositionSlider_UserPositionChanged(object sender, TimeSpan e)
        {
            IPlaylist playlist = viewModel.Audio?.CurrentPlaylist;
            if (playlist != null) playlist.WannaSong = RequestSong.Get(playlist.CurrentSong, e, playlist.Duration);
        }

        private async void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToSettingsPage();
        }

        private void AbbPrevious_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Audio?.SetPreviousSong();
        }

        private void AbbPlayPause_Click(object sender, RoutedEventArgs e)
        {
            IAudioService service = viewModel.Audio;

            if (service == null) return;

            service.PlayState = service.PlayState == PlaybackState.Playing
                ? PlaybackState.Paused
                : PlaybackState.Playing;
        }

        private void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Audio?.SetNextSong();
        }

        private async void AbbSettings_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.Audio != null)
            {
                serviceHandler.Builder.WithService(viewModel.Audio);
            }

            await NavigateToSettingsPage();
        }

        private async Task NavigateToSettingsPage()
        {
            TaskCompletionSourceS<ServiceBuilder> result = new TaskCompletionSourceS<ServiceBuilder>(serviceHandler.Builder.Clone());
            Frame.Navigate(typeof(SettingsPage), result);

            ServiceBuilder newBuilder = await result.Task;

            if (newBuilder == null) return;

            serviceHandler.Builder = newBuilder;

            await serviceHandler.CloseAsync();
            await serviceHandler.ConnectAsync(true);
        }

        private async void AbbDebug_Click(object sender, RoutedEventArgs e)
        {
            await new MessageDialog(Settings.Current.SuspendTime.ToString(CultureInfo.InvariantCulture), "SuspendTime").ShowAsync();

            string exceptionText = Settings.Current.UnhandledExceptionText ?? "<null>";
            DateTime time = Settings.Current.UnhandledExceptionTime;

            await new MessageDialog(exceptionText, time.ToString()).ShowAsync();

            string message = $"Communicator: {serviceHandler?.Communicator?.Name}\r\n" +
                $"State: {serviceHandler?.Communicator?.IsOpen}\r\n" +
                $"Back: {AudioPlayerFrontend.Background.BackgroundTaskHandler.Current?.IsRunning}";
            await new MessageDialog(message).ShowAsync();
        }
    }
}
