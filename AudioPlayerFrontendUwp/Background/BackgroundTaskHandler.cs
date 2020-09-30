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
        private IAudioService audio;

        public bool IsRunning { get; private set; }

        public BackgroundTaskHandler(Dispatcher dispatcher, ServiceHandler service)
        {
            isInBackground = false;
            sem = new SemaphoreSlim(0, 1);

            this.dispatcher = dispatcher;
            this.service = service;
            service.PropertyChanged += Service_PropertyChanged;

            closeTimer = ResetTimer.Start(inativeTime);
            closeTimer.RanDown += CloseTimer_RanDown;

            Application.Current.EnteredBackground += OnEnteredBackground;
            Application.Current.LeavingBackground += OnLeavingBackground;

            audio = service.Audio;
        }

        private void Service_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ServiceHandler.Audio))
            {
                audio = service.Audio;
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
