using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using StdOttStandard;
using StdOttStandard.Dispatch;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.UI.Xaml;

namespace AudioPlayerFrontend.Background
{
    class BackgroundTaskHandler
    {
        private static readonly TimeSpan inativeTime = TimeSpan.FromMinutes(10);

        public static BackgroundTaskHandler Current { get; set; }

        public bool isInBackground;
        private readonly ServiceHandler service;
        private readonly SemaphoreSlim sem;
        private readonly Dispatcher dispatcher;
        private readonly ResetTimer closeTimer;
        //private readonly SystemMediaTransportControls smtc;
        private IAudioService audio;

        public bool IsRunning { get; private set; }

        public BackgroundTaskHandler(Dispatcher dispatcher, ServiceHandler service)
        {
            isInBackground = false;
            sem = new SemaphoreSlim(0, 1);

            this.dispatcher = dispatcher;
            this.service = service;
            service.PropertyChanged += Service_PropertyChanged;

            //smtc = GetInitializedSystemMediaTransportControls();
            //smtc.ButtonPressed += Smtc_ButtonPressed;

            closeTimer = ResetTimer.Start(inativeTime);
            closeTimer.RanDown += CloseTimer_RanDown;

            Application.Current.EnteredBackground += OnEnteredBackground;
            Application.Current.LeavingBackground += OnLeavingBackground;

            SetAudioService(service.Audio);
        }

        private void Service_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ServiceHandler.Audio))
            {
                SetAudioService(service.Audio);
            }
        }

        private static SystemMediaTransportControls GetInitializedSystemMediaTransportControls()
        {
            SystemMediaTransportControls smtc = SystemMediaTransportControls.GetForCurrentView();
            smtc.IsPlayEnabled = true;
            smtc.IsPauseEnabled = true;
            smtc.IsNextEnabled = true;
            smtc.IsPreviousEnabled = true;
            smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
            smtc.IsEnabled = true;

            return smtc;
        }

        private void SetAudioService(IAudioService audioService)
        {
            Unsubscribe(audio);

            audio = audioService;
            if (audio == null) return;

            Subscribe(audio);

            SetPlayState(audio.PlayState);
            DisplayCurrentSong(audio.CurrentPlaylist.CurrentSong);
        }

        private void Subscribe(IAudioService audio)
        {
            if (audio == null) return;

            audio.PlayStateChanged += Audio_PlayStateChanged;
            audio.CurrentPlaylistChanged += Audio_CurrentPlaylistChanged;

            Subscribe(audio.CurrentPlaylist);
        }

        private void Unsubscribe(IAudioService audio)
        {
            if (audio == null) return;

            audio.PlayStateChanged -= Audio_PlayStateChanged;
            audio.CurrentPlaylistChanged -= Audio_CurrentPlaylistChanged;

            Unsubscribe(audio.CurrentPlaylist);
        }

        private void Subscribe(IPlaylistBase currentPlaylist)
        {
            currentPlaylist.CurrentSongChanged += CurrentPlaylist_CurrentSongChanged;
        }

        private void Unsubscribe(IPlaylistBase currentPlaylist)
        {
            currentPlaylist.CurrentSongChanged -= CurrentPlaylist_CurrentSongChanged;
        }

        private void Audio_PlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            SetPlayState(e.NewValue);
        }

        private void SetPlayState(PlaybackState state)
        {
            //switch (state)
            //{
            //    case PlaybackState.Stopped:
            //        smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
            //        break;

            //    case PlaybackState.Playing:
            //        smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
            //        break;

            //    case PlaybackState.Paused:
            //        smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
            //        break;
            //}
        }

        private void Audio_CurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            Unsubscribe(e.OldValue);
            Subscribe(e.NewValue);

            DisplayCurrentSong(e.NewValue.CurrentSong);
        }

        private void CurrentPlaylist_CurrentSongChanged(object sender, ValueChangedEventArgs<Song?> e)
        {
            DisplayCurrentSong(e.NewValue);
        }

        private void DisplayCurrentSong(Song? song)
        {
            //smtc.DisplayUpdater.MusicProperties.Title = song?.Title ?? "<None>";
            //smtc.DisplayUpdater.MusicProperties.Artist = song?.Artist ?? "<None>";
            //smtc.DisplayUpdater.Update();
        }

        private void CloseTimer_RanDown(object sender, EventArgs e)
        {
            if (isInBackground && audio.PlayState != PlaybackState.Playing) Stop();
        }

        private async void OnEnteredBackground(object sender, object e)
        {
            isInBackground = true;
            await closeTimer.Reset();
        }

        private void OnLeavingBackground(object sender, object e)
        {
            isInBackground = false;
        }

        private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    break;
                case SystemMediaTransportControlsButton.Next:
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    break;
            }
        }

        public List<DateTime> runTimes = new List<DateTime>(), closeTimes = new List<DateTime>();
        public async Task Run()
        {
            runTimes.Add(DateTime.Now);
            IsRunning = true;
            ResetCloseTimer();

            dispatcher.Start();

            await sem.WaitAsync();
            IsRunning = false;
            closeTimes.Add(DateTime.Now);
            await Task.WhenAll(service.CloseAsync(), dispatcher.Stop());
        }

        private async void ResetCloseTimer()
        {
            await closeTimer.Reset();
        }

        public void Stop()
        {
            if (IsRunning) sem.Release();
        }
    }
}
