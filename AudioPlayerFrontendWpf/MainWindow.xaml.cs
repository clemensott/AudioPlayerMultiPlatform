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

namespace AudioPlayerFrontend
{
    public partial class MainWindow : Window
    {
        private readonly IFileSystemService fileSystemService;
        private ServiceBuilder serviceBuilder;
        private HotKeysBuilder hotKeysBuilder;
        private readonly ViewModel viewModel;
        private HotKeys hotKeys;
        private bool isChangingSelectedSongIndex;
        private readonly ObservableCollection<IPlaylist> allPlaylists;

        public MainWindow()
        {
            fileSystemService = AudioPlayerServiceProvider.Current.GetFileSystemService();
            allPlaylists = new ObservableCollection<IPlaylist>();

            InitializeComponent();

            RestoreWindowHandler.Activate(this, RestoreWindowSettings.GetDefault());

            serviceBuilder = new ServiceBuilder();
            hotKeysBuilder = new HotKeysBuilder();

            DataContext = viewModel = new ViewModel();

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
                    if (viewModel.Service.Communicator != null)
                    {
                        await OpenAudioServiceAsync();
                    }

                    Subscribe(hotKeys);
                    break;

                case PowerModes.Suspend:
                    Unsubscribe(hotKeys);

                    viewModel.Service?.Communicator?.Dispose();
                    break;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            try
            {
                serviceBuilder.WithArgs(args);
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

            if (result.TryGetFirstValidOptionParseds(disableUiOpt, out _)) viewModel.IsUiEnabled = false;

            await BuildAudioServiceAsync();

            BuildHotKeys();
        }

        private async Task BuildAudioServiceAsync()
        {
            while (true)
            {
                if (viewModel.Service?.Communicator != null) viewModel.Service.Communicator.Disconnected -= Communicator_Disconnected;
                viewModel.Service = null;

                ServiceBuild build = ServiceBuild.Build(serviceBuilder, TimeSpan.FromMilliseconds(500));
                await Task.WhenAny(build.CompleteToken.ResultTask, Task.Delay(100));

                if (build.CompleteToken.IsEnded == BuildEndedType.Canceled ||
                    (!build.CompleteToken.IsEnded.HasValue && ShowBuildOpenWindow(build) == false))
                {
                    build.Cancel();
                    Close();
                    return;
                }

                ServiceBuildResult result = await build.CompleteToken.ResultTask;
                viewModel.Service = result;

                if (build.CompleteToken.IsEnded is BuildEndedType.Settings) UpdateBuilders();
                else if (build.CompleteToken.IsEnded != BuildEndedType.Successful) continue;

                if (build.Communicator != null) build.Communicator.Disconnected += Communicator_Disconnected;
                if (!(build.Communicator is IClientCommunicator))
                {
                    await Task.Run(async () =>
                    {
                        await Task.WhenAll(result.AudioService.SourcePlaylists
                            .Select(playlist => fileSystemService.UpdateSourcePlaylist(playlist, result.AudioService.FileMediaSourceRoots)));

                        // remove all songs from not source playlists that are not in source playlists (any more)
                        IDictionary<string, Song> allSongs = result.AudioService.SourcePlaylists
                            .SelectMany(p => p.Songs).Distinct().ToDictionary(s => s.FullPath);

                        Song song = new Song();
                        foreach (IPlaylist playlist in result.AudioService.Playlists)
                        {
                            playlist.Songs = playlist.Songs
                                .Where(s => allSongs.TryGetValue(s.FullPath, out song)).Select(_ => song).ToArray();
                        }
                    });
                }
                break;
            }
        }

        private void Communicator_Disconnected(object sender, DisconnectedEventArgs e)
        {
            Logs.Log($"Disconnected: {e.OnDisconnect} | {e.Exception?.Message ?? "<none>"}");
            if (e.OnDisconnect) return;

            Dispatcher.Invoke(async () => await OpenAudioServiceAsync());
        }

        private async Task OpenAudioServiceAsync()
        {
            while (true)
            {
                if (viewModel.Service.Communicator != null) viewModel.Service.Communicator.Disconnected -= Communicator_Disconnected;

                ServiceBuild build = ServiceBuild.Open(viewModel.Service.Communicator, viewModel.Service.AudioService,
                    viewModel.Service.ServicePlayer, viewModel.Service.Data, TimeSpan.FromMilliseconds(500));
                await Task.WhenAny(build.CompleteToken.ResultTask, Task.Delay(100));

                if (build.CompleteToken.IsEnded == BuildEndedType.Canceled ||
                    (!build.CompleteToken.IsEnded.HasValue && ShowBuildOpenWindow(build) == false))
                {
                    build.Cancel();
                    Close();
                    return;
                }

                await build.CompleteToken.ResultTask;

                if (build.CompleteToken.IsEnded is BuildEndedType.Settings) UpdateBuilders();
                else if (build.CompleteToken.IsEnded is BuildEndedType.Successful)
                {
                    if (build.Communicator != null) build.Communicator.Disconnected += Communicator_Disconnected;
                    break;
                }
            }
        }

        private bool? ShowBuildOpenWindow(ServiceBuild build)
        {
            BuildOpenWindow window = BuildOpenWindow.Current;

            //BuildOpenWindow window = new BuildOpenWindow(build);
            window.Build = build;

            return window.ShowDialog();
        }

        private bool UpdateBuilders()
        {
            ServiceBuilder serviceBuilderEdit = serviceBuilder.Clone();
            HotKeysBuilder hotKeysBuilderEdit = hotKeysBuilder.Clone();

            if (viewModel?.Service?.AudioService != null) serviceBuilderEdit.WithService(viewModel.Service.AudioService);
            if (viewModel?.Service?.Communicator != null) serviceBuilderEdit.WithCommunicator(viewModel.Service.Communicator);
            if (hotKeys != null) hotKeysBuilderEdit.WithHotKeys(hotKeys);

            SettingsWindow window = new SettingsWindow(serviceBuilderEdit, hotKeysBuilderEdit);

            if (window.ShowDialog() != true) return false;

            serviceBuilder = serviceBuilderEdit;
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
                        viewModel.AudioServiceUI.AddSongsToFirstPlaylist(new Song[] { addSong }, prepend);
                        service.PlayState = PlaybackState.Playing;
                    }
                    break;

                case Key.Escape:
                    service.SearchKey = string.Empty;
                    break;

                case Key.Up:
                    if (lbxSongs.Items.Count > 0 && viewModel.AudioServiceUI?.IsSearching == true)
                    {
                        isChangingSelectedSongIndex = true;
                        lbxSongs.SelectedIndex =
                            StdUtils.OffsetIndex(lbxSongs.SelectedIndex, lbxSongs.Items.Count, -1).index;
                        isChangingSelectedSongIndex = false;
                    }
                    break;

                case Key.Down:
                    if (lbxSongs.Items.Count > 0 && viewModel.AudioServiceUI?.IsSearching == true)
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
            viewModel.Service?.AudioService.SetPreviousSong();
        }

        private void OnTogglePlayPause(object sender, EventArgs e)
        {
            IAudioService service = viewModel.Service?.AudioService;

            if (service == null) return;

            service.PlayState = service.PlayState == PlaybackState.Playing ?
                PlaybackState.Paused : PlaybackState.Playing;
        }

        private void OnNext(object sender, EventArgs e)
        {
            viewModel.Service?.AudioService.SetNextSong();
        }

        private void OnPlay(object sender, EventArgs e)
        {
            if (viewModel.Service?.AudioService != null) viewModel.Service.AudioService.PlayState = PlaybackState.Playing;
        }

        private void OnPause(object sender, EventArgs e)
        {
            if (viewModel.Service?.AudioService != null) viewModel.Service.AudioService.PlayState = PlaybackState.Paused;
        }

        private void OnRestart(object sender, EventArgs e)
        {
            if (viewModel.Service?.AudioService != null) viewModel.Service.AudioService.CurrentPlaylist.Position = TimeSpan.Zero;
        }

        private async void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!UpdateBuilders()) return;

            if (viewModel.Service.Communicator != null)
            {
                try
                {
                    await viewModel.Service.Communicator.CloseAsync();
                }
                catch { }
            }

            await BuildAudioServiceAsync();

            BuildHotKeys();
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            hotKeys?.Dispose();
            viewModel.Service?.ServicePlayer?.Dispose();
            viewModel.Service?.Communicator?.Dispose();
            viewModel.Service?.Data?.Dispose();
        }

        private void StackPanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            string message = $"Communicator: {viewModel.Service?.Communicator?.Name}\r\nState: {viewModel.Service?.Communicator?.IsOpen}";
            MessageBox.Show(message, "State");

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

        private void SplPlaylistItem_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            bool isPlaylistUpdateable = element.DataContext is ISourcePlaylist &&
                !(viewModel.Service.Communicator is IClientCommunicator);

            if (!isPlaylistUpdateable) return;

            foreach (FrameworkElement item in element.ContextMenu.Items.OfType<FrameworkElement>())
            {
                switch (item.Name)
                {
                    case "mimReloadSongs":
                    case "sprUpdatePlaylist":
                        item.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private async void MimReloadSongs_Click(object sender, RoutedEventArgs e)
        {
            FileMediaSourceRoot[] roots = viewModel.Service.AudioService.FileMediaSourceRoots;
            await fileSystemService.ReloadSourcePlaylist(FrameworkUtils.GetDataContext<ISourcePlaylist>(sender), roots);
        }

        private void MimRemixSongs_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = FrameworkUtils.GetDataContext<IPlaylist>(sender);
            playlist.Songs = playlist.Songs.Shuffle().ToArray();
        }

        private void MimRemovePlaylist_Click(object sender, RoutedEventArgs e)
        {
            IPlaylist playlist = FrameworkUtils.GetDataContext<IPlaylist>(sender);
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
