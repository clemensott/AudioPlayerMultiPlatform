using AudioPlayerBackend;
using AudioPlayerBackend.Common;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static readonly TimeSpan networkConnectionTimeOut = TimeSpan.FromMilliseconds(200),
            networkConnectionMaxTime = TimeSpan.FromSeconds(5);

        private Task<IAudioExtended> buildTask;
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

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (viewModel.AudioService != null || viewModel.IsTryOpening)
            {
                if (viewModel.AudioService == null && !viewModel.IsTryOpening) await viewModel.BuildAsync();
                //if (viewModel.AudioService is IMqttAudioClient mqttAudioClient)
                //{
                //    mqttAudioClient.MqttClient.Disconnected += MqttClient_Disconnected;
                //}
            }
            else Frame.Navigate(typeof(SettingsPage), viewModel.Builder);
        }

        private async void MqttClient_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                string message = string.Format("Was connected: {0}\r\nException: {1}",
                    e.ClientWasConnected, e.Exception?.ToString() ?? "null");
                await new MessageDialog(message, "MqttClient_Disconnected").ShowAsync();

                IMqttAudioClient mqtttAudioClient = viewModel.AudioService?.Base as IMqttAudioClient;
                mqtttAudioClient.MqttClient.Disconnected -= MqttClient_Disconnected;

                if (await viewModel.OpenAsync(mqtttAudioClient)) mqtttAudioClient.MqttClient.Disconnected += MqttClient_Disconnected;
            });
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.Reload();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.AudioService != null) Frame.Navigate(typeof(SearchPage), viewModel);
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

        private void AbbPrevious_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.SetPreviousSong();
        }

        private void AbbPlayPause_Click(object sender, RoutedEventArgs e)
        {
            AudioViewModel audio = viewModel.AudioService;

            if (audio == null) return;

            audio.PlayState = audio.PlayState == PlaybackState.Playing ? PlaybackState.Paused : PlaybackState.Playing;
        }

        private void AbbNext_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.SetNextSong();
        }

        private async void AbbSettings_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.AudioService != null)
            {
                viewModel.Builder.WithService(viewModel.AudioService.Base);

                try
                {
                    if (viewModel.AudioService is IMqttAudio mqtttAudio) await mqtttAudio.CloseAsync();
                }
                catch { }
            }

            Frame.Navigate(typeof(SettingsPage), viewModel.Builder);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            viewModel.IsTryOpening = false;
        }

        private object MulDoRemove_Convert(object input0, object input1)
        {
            return !ReferenceEquals(input0, input1);
        }

        private object MulViewSongs_Convert(object input0, object input1, object input2, object input3)
        {
            IPlaylistExtended fileBasePlaylist = (IPlaylistExtended)input0;
            IEnumerable<Song> fileBasePlaylistViewSongs = (IEnumerable<Song>)input1;
            IPlaylistExtended currentPlaylist = (IPlaylistExtended)input2;
            IEnumerable<Song> currentPlaylistViewSongs = (IEnumerable<Song>)input3;

            return fileBasePlaylist?.IsSearching == true && fileBasePlaylist != currentPlaylist ?
                fileBasePlaylistViewSongs?.Except(currentPlaylistViewSongs.ToNotNull()) : currentPlaylistViewSongs;
        }

        private void SyiRemove_Tapped(object sender, TappedRoutedEventArgs e)
        {
            IPlaylistExtended playlist;
            Song song = (Song)((FrameworkElement)sender).DataContext;
            AudioViewModel service = viewModel.AudioService;

            if (service == null) return;

            if ((bool?)mulDoRemove.Output == true)
            {
                if (!service.Base.AdditionalPlaylists.TryFirst(out playlist))
                {
                    playlist = new Playlist
                    {
                        Loop = LoopType.Next,
                        Songs = new Song[] { song },
                        CurrentSong = song
                    };

                    service.Base.AdditionalPlaylists.Add(playlist);
                }
                else playlist.Songs = playlist.Songs.Concat(song).ToArray();

                service.Base.CurrentPlaylist = playlist;
            }
            else if (service.AdditionalPlaylists.TryFirst(out playlist))
            {
                playlist.Songs = playlist.Songs.Where(s => s != song).ToArray();

                if (playlist.Songs.Length == 0)
                {
                    service.Base.AdditionalPlaylists.Remove(playlist);
                    service.Base.CurrentPlaylist = service.GetAllPlaylists().First();
                }
            }
        }

        private void BtnLoopType_Click(object sender, RoutedEventArgs e)
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
                    viewModel.AudioService.CurrentPlaylist.Loop = LoopType.Next;
                    break;
            }
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
