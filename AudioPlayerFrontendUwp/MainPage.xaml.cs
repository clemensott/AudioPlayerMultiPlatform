using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

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
            if (e.Parameter is ViewModel)
            {
                DataContext = viewModel = e.Parameter as ViewModel;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (viewModel.IsTryOpening) await viewModel.BuildOrOpenTask;
            else if (viewModel.AudioService == null) await viewModel.BuildAsync();

            if (viewModel.AudioService == null) Frame.Navigate(typeof(SettingsPage), viewModel.Builder);
            //else if (viewModel.AudioService is IMqttAudioClient mqttAudioClient)
            //{
            //    mqttAudioClient.MqttClient.Disconnected += MqttClient_Disconnected;
            //}

            Scroll();
        }

        //private async void MqttClient_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        //{
        //    await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
        //    {
        //        string message = string.Format("Was connected: {0}\r\nException: {1}",
        //            e.ClientWasConnected, e.Exception?.ToString() ?? "null");
        //        await new MessageDialog(message, "MqttClient_Disconnected").ShowAsync();

        //        IMqttAudioClient mqttAudioClient = viewModel.AudioService?.Base as IMqttAudioClient;
        //        mqttAudioClient.MqttClient.Disconnected -= MqttClient_Disconnected;

        //        if (await viewModel.OpenAsync(mqttAudioClient)) mqttAudioClient.MqttClient.Disconnected += MqttClient_Disconnected;
        //    });
        //}

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AudioService?.SourcePlaylist.Reload();
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

                try
                {
                    if (viewModel.Communicator != null) await viewModel.Communicator.CloseAsync();
                }
                catch { }
            }

            Frame.Navigate(typeof(SettingsPage), viewModel.Builder);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CancelBuildOrOpen();
        }

        private object MulDoRemove_Convert(object input0, object input1, int changedIndex)
        {
            return !ReferenceEquals(input0, input1);
        }

        private object MulViewSongs_Convert(object input0, object input1, object input2, object input3)
        {
            ISourcePlaylist sourcePlaylist = (ISourcePlaylist)input0;
            IEnumerable<Song> fileBasePlaylistViewSongs = (IEnumerable<Song>)input1;
            IPlaylist currentPlaylist = (IPlaylist)input2;
            IEnumerable<Song> currentPlaylistViewSongs = (IEnumerable<Song>)input3;

            return sourcePlaylist?.IsSearching == true && sourcePlaylist != currentPlaylist ?
                fileBasePlaylistViewSongs?.Except(currentPlaylistViewSongs.ToNotNull()) : currentPlaylistViewSongs;
        }

        private void SyiRemove_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Song song = (Song)((FrameworkElement)sender).DataContext;
            IAudioService service = viewModel.AudioService;

            if (service.CurrentPlaylist.Songs.All(s => s == song))
            {
                service.Playlists.Remove(service.CurrentPlaylist);
            }
            else service.CurrentPlaylist.Songs = service.CurrentPlaylist.Songs.Where(s => s != song).ToArray();
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

        private void SplCurrentSong_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Scroll();
        }

        private object MicCurrentSongIndex_ConvertRef(ref object input0, ref object input1, ref object input2, ref object input3, int changedInput)
        {
            if (input0 == null) return null;

            IEnumerable<Song> allSongs = (IEnumerable<Song>)input0;
            Song? currentSong = (Song?)input1;
            int index = (int)input3;

            input2 = allSongs;

            if (changedInput == 3 && index != -1) input1 = allSongs.ElementAt(index);
            else if (!currentSong.HasValue) input3 = -1;
            else input3 = allSongs.IndexOf(currentSong.Value);

            return null;
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
