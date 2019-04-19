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

namespace AudioPlayerFrontend
{
    public partial class MainWindow : Window
    {
        private static readonly TimeSpan buildOpenDelay = TimeSpan.FromMilliseconds(300);

        private readonly ServiceBuilder serviceBuilder;
        private readonly HotKeysBuilder hotKeysBuilder;
        private readonly ViewModel viewModel;
        private HotKeys hotKeys;
        private WidthService widthService;

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

                    if (viewModel.CommunicatorUI != null)
                    {
                        try
                        {
                            await viewModel.CommunicatorUI.CloseAsync();
                        }
                        catch { }
                    }
                    break;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            serviceBuilder.WithArgs(args).WithPlayer(new Player(-1, windowHandle));
            hotKeysBuilder.WithArgs(args);

            Option disableUiOpt = Option.GetLongOnly("disable-ui", "Disables UI on startup.", false, 0);
            OptionParseResult result = new Options(disableUiOpt).Parse(args);

            if (result.TryGetFirstValidOptionParseds(disableUiOpt, out _)) viewModel.IsUiEnabled = false;

            await BuildAudioServiceAsync();

            BuildHotKeys();
        }

        public async Task BuildAudioServiceAsync()
        {
            while (true)
            {
                BuildStatusToken statusToken = new BuildStatusToken();
                Task<ServiceBuildResult> task = serviceBuilder.BuildWhileAsync(statusToken, TimeSpan.FromMilliseconds(500));

                if (await AwaitTaskOrDelay(task, buildOpenDelay) && ShowBuildOpenWindow(statusToken) == false)
                {
                    statusToken.End(BuildEndedType.Canceled);
                    Close();
                    return;
                }

                viewModel.Service = await task;

                if (statusToken.IsEnded is BuildEndedType.Successful) break;
                if (statusToken.IsEnded is BuildEndedType.Settings) UpdateBuilders();
            }

            if (Visibility != Visibility.Visible) Show();
        }

        public async Task OpenAudioServiceAsync()
        {
            Hide();

            while (true)
            {
                BuildStatusToken statusToken = new BuildStatusToken();
                Task task = viewModel.CommunicatorUI.OpenWhileAsync(statusToken, TimeSpan.FromMilliseconds(500));

                await AwaitTaskOrDelay(task, TimeSpan.FromMilliseconds(300));

                if (await AwaitTaskOrDelay(task, buildOpenDelay) && ShowBuildOpenWindow(statusToken) == false)
                {
                    statusToken.End(BuildEndedType.Canceled);
                    Close();
                    return;
                }

                await task;

                if (statusToken.IsEnded is BuildEndedType.Successful) break;
                if (statusToken.IsEnded is BuildEndedType.Settings) UpdateBuilders();
            }

            Show();
        }

        private async Task<bool> AwaitTaskOrDelay(Task task, TimeSpan delay)
        {
            if (Visibility != Visibility.Visible) return false;

            Task finishedTask = await Task.WhenAny(task, Task.Delay(delay));

            return task != finishedTask;
        }

        private bool? ShowBuildOpenWindow(BuildStatusToken statusToken)
        {
            BuildOpenWindow window = BuildOpenWindow.Current;

            //BuildOpenWindow window = new BuildOpenWindow(statusToken);
            window.StatusToken = statusToken;

            Hide();
            return window.ShowDialog();
        }

        private void UpdateBuilders()
        {
            if (viewModel?.Service?.AudioService != null) serviceBuilder.WithService(viewModel.Service.AudioService);
            if (viewModel?.Service?.Communicator != null) serviceBuilder.WithCommunicator(viewModel.Service.Communicator);
            if (viewModel?.Service?.ServicePlayer != null) serviceBuilder.WithPlayerService(viewModel.Service.ServicePlayer);
            if (hotKeys != null) hotKeysBuilder.WithHotKeys(hotKeys);

            SettingsWindow window = new SettingsWindow(serviceBuilder, hotKeysBuilder);
            window.ShowDialog();
        }

        private void BuildHotKeys()
        {
            try
            {
                HotKeys hotKeys = hotKeysBuilder.Build();

                if (hotKeys != this.hotKeys)
                {
                    Unsubscribe(this.hotKeys);
                    this.hotKeys = hotKeys;
                    Subscribe(hotKeys);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(),
                    "Building hotkeys error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F3) tbxSearch.Focus();
        }

        private void TbxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            IAudioService service = viewModel.AudioServiceUI;

            if (service == null) return;

            switch (e.Key)
            {
                case Key.Enter:
                    if (service.SourcePlaylist.SearchSongs.Any())
                    {
                        viewModel.AudioServiceUI.AddSongsToFirstPlaylist(service.SourcePlaylist.SearchSongs.Take(1));
                        service.PlayState = PlaybackState.Playing;
                    }
                    break;

                case Key.Escape:
                    service.SourcePlaylist.SearchKey = string.Empty;
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
            UpdateBuilders();

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
        }

        private void StackPanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (viewModel.Service.ServicePlayer.Player is Player player)
            {
                string message = string.Format("ServiceState: {0}\r\nWaveOutPlayState: {1}\r\ncurrentWaveProvider {2}\r\n" +
                    "nextWaveProvider {3}\r\nStop: {4}\r\nStopped: {5}\r\n\r\nPlay WaveOut? (Yes)\r\nPlay Service? (No)",
                    viewModel?.Service?.AudioService?.PlayState, player.waveOut.PlaybackState, GetFileName(player.waveProvider),
                    GetFileName(player.nextWaveProvider), player.stop, player.stopped);
                MessageBoxResult result = MessageBox.Show(message, "State", MessageBoxButton.YesNoCancel);

                try
                {
                    if (result == MessageBoxResult.Yes)
                    {
                        player.waveOut.Play();
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        viewModel.Service.AudioService.PlayState = PlaybackState.Playing;
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
            ref object input3, ref object input4, ref object input5, ref object input6, int changedInput)
        {
            if (input0 == null || input1 == null || input3 == null || input4 == null || input6 == null)
            {
                input5 = null;
                return null;
            }

            IPlaylist currentPlaylist = (IPlaylist)input0;
            IEnumerable<Song> allSongs = (IEnumerable<Song>)input1;
            Song? currentSong = (Song?)input2;
            IEnumerable<Song> searchSongs = (IEnumerable<Song>)input3;
            bool isSearching = (bool)input4;
            int index = (int)input6;
            IEnumerable<Song> songs = isSearching ? searchSongs : allSongs;

            input5 = songs;

            if (changedInput == 6 && !isSearching && index != -1) input2 = songs.ElementAt(index);
            else if (!currentSong.HasValue) input6 = -1;
            else if (songs.Contains(currentSong.Value)) input6 = songs.IndexOf(currentSong.Value);

            return isSearching || currentPlaylist.ID == Guid.Empty;
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
