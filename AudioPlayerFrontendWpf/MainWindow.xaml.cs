using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using AudioPlayerFrontend.Join;
using Microsoft.Win32;
using StdOttStandard;
using StdOttStandard.CommendlinePaser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using AudioPlayerBackend.Build;

namespace AudioPlayerFrontend
{
    public partial class MainWindow : Window
    {
        private ServiceBuilder serviceBuilder;
        private HotKeysBuilder hotKeysBuilder;
        private readonly ViewModel viewModel;
        private HotKeys hotKeys;
        private readonly WidthService widthService;
        private bool isChangingSelectedSongIndex;

        public MainWindow()
        {
            InitializeComponent();

            serviceBuilder = new ServiceBuilder(ServiceBuilderHelper.Current);
            hotKeysBuilder = new HotKeysBuilder();

            DataContext = viewModel = new ViewModel();
            widthService = new WidthService(tblTitle, tblArtist, cdSong, sldPosition);

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
                    if (viewModel.CommunicatorUI != null)
                    {
                        await OpenAudioServiceAsync();
                    }

                    Subscribe(hotKeys);
                    break;

                case PowerModes.Suspend:
                    Unsubscribe(hotKeys);

                    if (viewModel.Service.Communicator != null)
                    {
                        try
                        {
                            await viewModel.Service.Communicator.CloseAsync();
                        }
                        catch { }
                    }
                    break;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            serviceBuilder.WithArgs(args);
            hotKeysBuilder.WithArgs(args);

            Option disableUiOpt = Option.GetLongOnly("disable-ui", "Disables UI on startup.", false, 0);
            OptionParseResult result = new Options(disableUiOpt).Parse(args);

            if (result.TryGetFirstValidOptionParseds(disableUiOpt, out _)) viewModel.IsUiEnabled = false;

            await BuildAudioServiceAsync();

            BuildHotKeys();
        }

        private async Task BuildAudioServiceAsync()
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            serviceBuilder.WithPlayer(new Player(-1, windowHandle));

            while (true)
            {
                ServiceBuild build = ServiceBuild.Build(serviceBuilder, TimeSpan.FromMilliseconds(500), AudioServiceHelper.Current);

                if (ShowBuildOpenWindow(build) == false)
                {
                    build.Cancel();
                    Close();
                    return;
                }

                viewModel.Service = await build.CompleteToken.ResultTask;

                if (build.CompleteToken.IsEnded is BuildEndedType.Successful) break;
                if (build.CompleteToken.IsEnded is BuildEndedType.Settings) UpdateBuilders();
            }

            if (Visibility != Visibility.Visible) Show();
        }

        private async Task OpenAudioServiceAsync()
        {
            Hide();

            while (true)
            {
                ServiceBuild build = ServiceBuild.Open(viewModel.Service.Communicator, viewModel.Service.AudioService,
                    viewModel.Service.ServicePlayer, viewModel.Service.Data, TimeSpan.FromMilliseconds(500));

                if (ShowBuildOpenWindow(build) == false)
                {
                    build.Cancel();
                    Close();
                    return;
                }

                await build.CompleteToken.ResultTask;

                if (build.CompleteToken.IsEnded is BuildEndedType.Successful) break;
                if (build.CompleteToken.IsEnded is BuildEndedType.Settings) UpdateBuilders();
            }

            Show();
        }

        private bool? ShowBuildOpenWindow(ServiceBuild build)
        {
            BuildOpenWindow window = BuildOpenWindow.Current;

            //BuildOpenWindow window = new BuildOpenWindow(build);
            window.Build = build;

            Hide();
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
                    service.SourcePlaylist.SearchKey = string.Empty;
                    break;

                case Key.Up:
                    if (lbxSongs.Items.Count > 0 && viewModel.AudioServiceUI?.SourcePlaylist.IsSearching == true)
                    {
                        isChangingSelectedSongIndex = true;
                        lbxSongs.SelectedIndex =
                            Utils.OffsetIndex(lbxSongs.SelectedIndex, lbxSongs.Items.Count, -1).index;
                        isChangingSelectedSongIndex = false;
                    }
                    break;

                case Key.Down:
                    if (lbxSongs.Items.Count > 0 && viewModel.AudioServiceUI?.SourcePlaylist.IsSearching == true)
                    {
                        isChangingSelectedSongIndex = true;
                        lbxSongs.SelectedIndex =
                            Utils.OffsetIndex(lbxSongs.SelectedIndex, lbxSongs.Items.Count, 1).index;
                        isChangingSelectedSongIndex = false;
                    }
                    break;

                default:
                    e.Handled = false;
                    break;
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Service?.AudioService.SourcePlaylist.Reload();
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
                viewModel.Service.AudioService.SourcePlaylist.FileMediaSources = sources;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            hotKeys?.Dispose();

            viewModel.CommunicatorUI?.Dispose();
            viewModel.Service?.Data?.Dispose();
        }

        private void StackPanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (viewModel.Service.ServicePlayer.Player is Player player)
            {
                string message = string.Format("ServiceState: {0}\r\nWaveOutPlayState: {1}\r\ncurrentWaveProvider {2}\r\n" +
                     "nextWaveProvider {3}\r\nStop: {4}\r\nStopped: {5}\r\nDebug: {6}\r\n\r\nPlay WaveOut? (Yes)\r\nPlay Service? (No)",
                     viewModel?.Service?.AudioService?.PlayState, player.waveOut.PlaybackState, GetFileName(player.waveProvider),
                     GetFileName(player.nextWaveProvider), player.stop, player.stopped, player.debug);
                MessageBoxResult result = MessageBox.Show(message, "State", MessageBoxButton.YesNoCancel);

                try
                {
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            player.waveOut.Play();
                            break;
                        case MessageBoxResult.No:
                            viewModel.Service.AudioService.PlayState = PlaybackState.Playing;
                            break;
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString(), "Exception");
                }
            }
        }

        private static string GetFileName(NAudio.Wave.IWaveProvider iwp)
        {
            if (iwp == null) return "null";

            if (iwp is WaveProvider wp)
            {
                if (wp.Parent is ReadEventWaveProvider rewp)
                {
                    if (rewp.Parent is AudioFileReader afr)
                    {
                        return afr.FileName;
                    }
                    else return "not AudiFileReader: " + rewp.GetType();
                }
                else return "not ReadEventWaveProvider: " + wp.GetType();
            }
            else return "not WaveProvider: " + iwp.GetType();
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

        private object MicCurrentSongIndex_ConvertRef(ref object input0, ref object input1, ref object input2,
            ref object input3, ref object input4, ref object input5, int changedInput)
        {
            if (input0 == null || input1 == null || input3 == null || input4 == null || input5 == null) return null;

            Song? currentSong = (Song?)input2;
            bool isSearching = (bool)input4;
            int indexLbx = (int)input5;

            object songsLbx = MicCurrentSongIndex_Convert((IEnumerable<Song>)input1,
                (IEnumerable<Song>)input3, isSearching, ref currentSong, ref indexLbx, changedInput);

            input2 = currentSong;
            input4 = isSearching;
            input5 = indexLbx;

            return songsLbx;
        }

        private object MicCurrentSongIndex_Convert(IEnumerable<Song> allSongs, IEnumerable<Song> searchSongs, bool isSearching,
            ref Song? currentSong, ref int lbxIndex, int changedInput)
        {
            IEnumerable<Song> songs;
            IPlaylist currentPlaylist = viewModel.AudioServiceUI?.CurrentPlaylist;
            IPlaylist sourcePlaylist = viewModel.AudioServiceUI?.SourcePlaylist;
            bool isCurrentPlaylistSourcePlaylist = currentPlaylist?.ID == sourcePlaylist?.ID;

            if (!isSearching) songs = allSongs;
            else if (isCurrentPlaylistSourcePlaylist) songs = searchSongs;
            else songs = searchSongs.Except(allSongs);

            if (changedInput == 5 && lbxIndex != -1 && isChangingSelectedSongIndex) ;
            else if (changedInput == 5 && lbxIndex != -1 && allSongs.Contains(songs.ElementAt(lbxIndex)))
            {
                currentSong = songs.ElementAt(lbxIndex);
            }
            else if (!currentSong.HasValue) lbxIndex = -1;
            else if (songs.Contains(currentSong.Value)) lbxIndex = songs.IndexOf(currentSong.Value);
            else if (songs.Any()) lbxIndex = 0;
            else lbxIndex = -1;

            return songs;
        }

        private object MicShuffle_ConvertRef(ref object input0, ref object input1, ref object input2, ref object input3, int changedInput)
        {
            if (input0 == null || input1 == null || input2 == null)
            {
                input3 = false;
                return null;
            }

            bool isSearching = (bool)input0;
            bool isAllShuffle = (bool)input1;
            bool isSearchShuffle = (bool)input2;
            bool? isShuffle = (bool?)input3;

            if (changedInput == 3 && isShuffle.HasValue)
            {
                if (isSearching) input2 = isShuffle;
                else input1 = isShuffle;
            }
            else input3 = isSearching ? isSearchShuffle : isAllShuffle;

            return null;
        }

        private void BtnLoop_Click(object sender, RoutedEventArgs e)
        {
            switch (viewModel.AudioServiceUI?.CurrentPlaylist.Loop)
            {
                case LoopType.Next:
                    viewModel.AudioServiceUI.CurrentPlaylist.Loop = LoopType.Stop;
                    break;

                case LoopType.Stop:
                    viewModel.AudioServiceUI.CurrentPlaylist.Loop = LoopType.CurrentPlaylist;
                    break;

                case LoopType.CurrentPlaylist:
                    viewModel.AudioServiceUI.CurrentPlaylist.Loop = LoopType.CurrentSong;
                    break;

                case LoopType.CurrentSong:
                    viewModel.AudioServiceUI.CurrentPlaylist.Loop = LoopType.Next;
                    break;
            }
        }
    }
}
