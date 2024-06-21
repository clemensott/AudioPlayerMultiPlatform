using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using Microsoft.Win32;
using StdOttStandard;
using StdOttStandard.Linq;
using StdOttStandard.CommandlineParser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AudioPlayerBackend.Build;
using StdOttFramework.RestoreWindow;
using StdOttStandard.Converter.MultipleInputs;
using AudioPlayerBackend.Communication;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using StdOttFramework;
using StdOttFramework.Converters;
using AudioPlayerBackend.FileSystem;
using AudioPlayerBackend.Audio.MediaSource;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AudioPlayerFrontend.Join;
using AudioPlayerBackend.ViewModels;
using AudioPlayerBackend.AudioLibrary;
using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo;

namespace AudioPlayerFrontend
{
    public partial class MainWindow : Window
    {
        private readonly AudioServicesHandler audioServicesHandler;
        private AudioServicesBuildConfig servicesBuildConfig;
        private HotKeysBuilder hotKeysBuilder;
        private AudioServices audioServices;
        private ILibraryViewModel viewModel;
        private HotKeys hotKeys;
        private bool isChangingSelectedSongIndex;
        private readonly ObservableCollection<IPlaylist> allPlaylists;

        public MainWindow()
        {
            allPlaylists = new ObservableCollection<IPlaylist>();

            InitializeComponent();

            RestoreWindowHandler.Activate(this, RestoreWindowSettings.GetDefault());

            audioServicesHandler = new AudioServicesHandler();
            servicesBuildConfig = new AudioServicesBuildConfig();
            hotKeysBuilder = new HotKeysBuilder();

            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor
                .FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
            dpd.AddValueChanged(lbxSongs, OnItemsSourceChanged);

            SystemEvents.PowerModeChanged += OnPowerChange;
        }

        private void OnItemsSourceChanged(object sender, EventArgs e)
        {
            Scroll();
        }

        private async void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    await (audioServices?.Start() ?? Task.CompletedTask);

                    Subscribe(hotKeys);
                    break;

                case PowerModes.Suspend:
                    Unsubscribe(hotKeys);

                    await (audioServices?.Stop() ?? Task.CompletedTask);
                    break;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            try
            {
                servicesBuildConfig.WithArgs(args);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Create service builder error");
                Close();
                return;
            }

            try
            {
                hotKeysBuilder.WithArgs(args);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Create hotkeys builder error");
                Close();
                throw;
            }

            Option disableUiOpt = Option.GetLongOnly("disable-ui", "Disables UI on startup.", false, 0);
            OptionParseResult result = new Options(disableUiOpt).Parse(args);

            //if (result.TryGetFirstValidOptionParseds(disableUiOpt, out _)) viewModel.IsUiEnabled = false;

            servicesBuildConfig.AdditionalServices.TryAddSingleton<IFileSystemService, FileSystemService>();
            servicesBuildConfig.AdditionalServices.TryAddSingleton<IInvokeDispatcherService, InvokeDispatcherService>();

            audioServicesHandler.ServicesBuild += AudioServicesHandler_ServicesBuild;
            audioServicesHandler.Stopped += AudioServicesHandler_Stopped;

            audioServicesHandler.Start(servicesBuildConfig);
        }

        private async void AudioServicesHandler_ServicesBuild(object sender, AudioServicesBuilder build)
        {
            await Task.WhenAny(build.CompleteToken.ResultTask, Task.Delay(100));

            if (build.CompleteToken.IsEnded == BuildEndedType.Canceled ||
                (!build.CompleteToken.IsEnded.HasValue && ShowBuildOpenWindow(build) == false))
            {
                build.Cancel();
                Close();
                return;
            }

            AudioServices newAudioServices = await build.CompleteToken.ResultTask;

            if (build.CompleteToken.IsEnded is BuildEndedType.Settings) await StopAndUpadateBuilder();
            else if (newAudioServices != null)
            {
                BuildHotKeys();
                audioServices = newAudioServices;
                DataContext = viewModel = audioServices.ServiceProvider.GetService<ILibraryViewModel>();
            }
        }

        private void AudioServicesHandler_Stopped(object sender, EventArgs e)
        {
        }

        private bool? ShowBuildOpenWindow(AudioServicesBuilder build)
        {
            BuildOpenWindow window = BuildOpenWindow.Current;

            //BuildOpenWindow window = new BuildOpenWindow(build);
            window.Build = build;

            return window.ShowDialog();
        }

        private async Task<bool> StopAndUpadateBuilder()
        {
            await audioServicesHandler.Stop();

            try
            {
                return UpdateBuilders();
            }
            finally
            {
                audioServicesHandler.Start(servicesBuildConfig);
            }
        }

        private bool UpdateBuilders()
        {
            AudioServicesBuildConfig serviceBuilderEdit = servicesBuildConfig.Clone();
            HotKeysBuilder hotKeysBuilderEdit = hotKeysBuilder.Clone();

            //if (viewModel?.Service?.AudioService != null) serviceBuilderEdit.WithService(viewModel.Service.AudioService);
            if (hotKeys != null) hotKeysBuilderEdit.WithHotKeys(hotKeys);

            SettingsWindow window = new SettingsWindow(serviceBuilderEdit, hotKeysBuilderEdit);

            if (window.ShowDialog() != true) return false;

            servicesBuildConfig = serviceBuilderEdit;
            hotKeysBuilder = hotKeysBuilderEdit;
            return true;
        }

        private void BuildHotKeys()
        {
            try
            {
                HotKeys newHotKeys = hotKeysBuilder.Build();

                Unsubscribe(hotKeys);
                hotKeys = newHotKeys;
                Subscribe(newHotKeys);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(),
                    "Building HotKeys error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F3) tbxSearch.Focus();
        }

        private void TbxSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            IAudioService service = viewModel.AudioServiceUI;

            if (service == null) return;

            e.Handled = true;

            switch (e.Key)
            {
                case Key.Enter:
                    if (lbxSongs.SelectedItem is Song)
                    {
                        bool prepend = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                        Song addSong = (Song)lbxSongs.SelectedItem;
                        service.AddSongsToFirstPlaylist(new Song[] { addSong }, prepend);
                        service.PlayState = PlaybackState.Playing;
                    }
                    break;

                case Key.Escape:
                    service.SearchKey = string.Empty;
                    break;

                case Key.Up:
                    if (lbxSongs.Items.Count > 0 && service?.IsSearching == true)
                    {
                        isChangingSelectedSongIndex = true;
                        lbxSongs.SelectedIndex =
                            StdUtils.OffsetIndex(lbxSongs.SelectedIndex, lbxSongs.Items.Count, -1).index;
                        isChangingSelectedSongIndex = false;
                    }
                    break;

                case Key.Down:
                    if (lbxSongs.Items.Count > 0 && service?.IsSearching == true)
                    {
                        isChangingSelectedSongIndex = true;
                        lbxSongs.SelectedIndex =
                            StdUtils.OffsetIndex(lbxSongs.SelectedIndex, lbxSongs.Items.Count, 1).index;
                        isChangingSelectedSongIndex = false;
                    }
                    break;

                default:
                    e.Handled = false;
                    break;
            }
        }

        private void OnPrevious(object sender, EventArgs e)
        {
            viewModel.CurrentPlaylist.SetPreviousSong();
        }

        private void OnTogglePlayPause(object sender, EventArgs e)
        {
            viewModel.SetTogglePlayState();
        }

        private void OnNext(object sender, EventArgs e)
        {
           viewModel.CurrentPlaylist.SetNextSong();
        }

        private void OnPlay(object sender, EventArgs e)
        {
            viewModel.SetPlay();
        }

        private void OnPause(object sender, EventArgs e)
        {
            viewModel.SetPause();
        }

        private void OnRestart(object sender, EventArgs e)
        {
            viewModel?.CurrentPlaylist.SetRestartCurrentSong();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (UpdateBuilders()) audioServicesHandler.Start(servicesBuildConfig);
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

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (viewModel.Service?.AudioService != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] sources = (string[])e.Data.GetData(DataFormats.FileDrop);
                Window addWindow = new AddSourcePlaylistWindow(sources, viewModel.Service.AudioService);
                addWindow.ShowDialog();
            }
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            audioServicesHandler.ServicesBuild -= AudioServicesHandler_ServicesBuild;
            audioServicesHandler.Stopped -= AudioServicesHandler_Stopped;

            hotKeys?.Dispose();
            await audioServicesHandler.Stop();
        }

        private void StackPanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //string message = $"Communicator: {viewModel.Service?.Communicator?.Name}\r\nState: {viewModel.Service?.Communicator?.IsOpen}";
            //MessageBox.Show(message, "State");

            MessageBoxResult clearLogsResult = MessageBox.Show(Logs.Get() + "\r\nClear?", "Logs", MessageBoxButton.YesNo);
            if (clearLogsResult == MessageBoxResult.Yes) Logs.Clear();
        }

        private void Subscribe(HotKeys hotKeys)
        {
            if (hotKeys == null) return;

            hotKeys.Toggle_Pressed += OnTogglePlayPause;
            hotKeys.Next_Pressed += OnNext;
            hotKeys.Previous_Pressed += OnPrevious;
            hotKeys.Play_Pressed += OnPlay;
            hotKeys.Pause_Pressed += OnPause;
            hotKeys.Restart_Pressed += OnRestart;

            hotKeys.Register();
        }

        private void Unsubscribe(HotKeys hotKeys)
        {
            if (hotKeys == null) return;

            hotKeys.Unregister();

            hotKeys.Toggle_Pressed -= OnTogglePlayPause;
            hotKeys.Next_Pressed -= OnNext;
            hotKeys.Previous_Pressed -= OnPrevious;
            hotKeys.Play_Pressed -= OnPlay;
            hotKeys.Pause_Pressed -= OnPause;
            hotKeys.Restart_Pressed -= OnRestart;
        }

        private void StpCurrentSong_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Scroll();
        }

        private object MicCurrentSongIndex_ConvertRef(object sender, MultiplesInputsConvert7EventArgs args)
        {
            if (args.Input0 == null || args.Input3 == null || args.Input4 == null || args.Input5 == null || args.Input6 == null) return null;

            Song? currentSong = (Song?)args.Input1;
            RequestSong? wannaSong = (RequestSong?)args.Input2;
            IEnumerable<Song> allSongs = (IEnumerable<Song>)args.Input3;
            IEnumerable<Song> searchSongs = (IEnumerable<Song>)args.Input4;
            bool isSearching = (bool)args.Input5;
            int indexLbx = (int)args.Input6;

            object songsLbx = MicCurrentSongIndex_ConvertRef(currentSong, ref wannaSong,
                allSongs, searchSongs, isSearching, ref indexLbx, args.ChangedValueIndex);

            args.Input2 = wannaSong;
            args.Input6 = indexLbx;

            return songsLbx;
        }

        private object MicCurrentSongIndex_ConvertRef(Song? currentSong, ref RequestSong? wannaSong,
            IEnumerable<Song> allSongs, IEnumerable<Song> searchSongs, bool isSearching, ref int lbxIndex, int changedInput)
        {
            IEnumerable<Song> songs;
            IPlaylist currentPlaylist = viewModel.AudioServiceUI?.CurrentPlaylist;
            bool isCurrentPlaylistSourcePlaylist = currentPlaylist is ISourcePlaylist;

            if (!isSearching) songs = allSongs;
            else if (isCurrentPlaylistSourcePlaylist) songs = searchSongs;
            else songs = searchSongs.Except(allSongs);

            if (changedInput == 6 && lbxIndex != -1 && (isSearching || isChangingSelectedSongIndex)) ;
            else if (changedInput == 6 && lbxIndex != -1 && allSongs.Contains(songs.ElementAt(lbxIndex)))
            {
                wannaSong = RequestSong.Start(songs.ElementAt(lbxIndex));
            }
            else if (!currentSong.HasValue) lbxIndex = -1;
            else if (songs.Contains(currentSong.Value)) lbxIndex = songs.IndexOf(currentSong.Value);
            else if (songs.Any()) lbxIndex = 0;
            else lbxIndex = -1;

            return songs;
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

        private object ValueConverter_ConvertEvent(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PlaylistType playlistType = (PlaylistType)value;
            return viewModel.IsLocalFileMediaSource && playlistType.HasFlag(PlaylistType.SourcePlaylist)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void MimReloadSongs_Click(object sender, RoutedEventArgs e)
        {
            IFileSystemService fileSystemService = audioServices.GetFileSystemService();
            PlaylistInfo playlist = FrameworkUtils.GetDataContext<PlaylistInfo>(sender);
            await fileSystemService.ReloadSourcePlaylist(playlist.Id);
        }

        private void MimRemixSongs_Click(object sender, RoutedEventArgs e)
        {
            PlaylistInfo playlist = FrameworkUtils.GetDataContext<PlaylistInfo>(sender);
            Playlist 
            playlist.Songs = playlist.Songs.Shuffle().ToArray();
        }

        private void MimRemovePlaylist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistInfo playlist = FrameworkUtils.GetDataContext<PlaylistInfo>(sender);
            IAudioService service = viewModel.Service.AudioService;

            if (service.CurrentPlaylist == playlist)
            {
                service.CurrentPlaylist = service.GetAllPlaylists().Where(p => p != playlist).Any() ?
                    service.GetAllPlaylists().Next(playlist).next : null;
            }

            if (playlist is ISourcePlaylist) service.SourcePlaylists.Remove((ISourcePlaylist)playlist);
            else service.Playlists.Remove(playlist);
        }

        private void SldPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            double positionSeconds = e.NewValue;
            double durationSeconds = slider.Maximum;
            IPlaylistBase currentPlaylist = viewModel.AudioServiceUI?.CurrentPlaylist;

            if (currentPlaylist != null && currentPlaylist.CurrentSong.HasValue &&
                Math.Abs(currentPlaylist.Duration.TotalSeconds - durationSeconds) < 0.01 &&
                Math.Abs(currentPlaylist.Position.TotalSeconds - positionSeconds) > 0.01)
            {
                currentPlaylist.WannaSong = RequestSong.Get(currentPlaylist.CurrentSong, TimeSpan.FromSeconds(positionSeconds));
            }
        }
    }
}
