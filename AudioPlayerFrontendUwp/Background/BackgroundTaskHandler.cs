using AudioPlayerBackend.Player;
using StdOttStandard;
using StdOttStandard.Dispatch;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.UI.Xaml;

namespace AudioPlayerFrontend.Background
{
    class BackgroundTaskHandler
    {
        private static readonly TimeSpan inativeTime = TimeSpan.FromMinutes(10);

        public static BackgroundTaskHandler Current { get; private set; }

        private bool isInBackground;
        private readonly ServiceHandler service;
        private readonly SemaphoreSlim sem;
        private readonly Dispatcher dispatcher;
        private readonly ResetTimer closeTimer;

        public bool IsRunning { get; private set; }

        public BackgroundTaskHandler(Dispatcher dispatcher, ServiceHandler service)
        {
            if (Current != null) throw new InvalidOperationException("Only one instance of this class is allowed");
            Current = this;

            isInBackground = false;
            sem = new SemaphoreSlim(0, 1);

            this.dispatcher = dispatcher;
            this.service = service;

            closeTimer = ResetTimer.Start(inativeTime);
            closeTimer.RanDown += CloseTimer_RanDown;

            Application.Current.EnteredBackground += OnEnteredBackground;
            Application.Current.LeavingBackground += OnLeavingBackground;
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
            if (isInBackground && service.Audio?.PlayState != PlaybackState.Playing) Stop();
            else ResetCloseTimer();
        }

        private void OnEnteredBackground(object sender, object e)
        {
            isInBackground = true;
            ResetCloseTimer();
        }

        private void OnLeavingBackground(object sender, object e)
        {
            isInBackground = false;
        }

        public async Task Run()
        {
            IsRunning = true;
            ResetCloseTimer();

            dispatcher.Start();

            await sem.WaitAsync();
            IsRunning = false;
            await dispatcher.Stop();
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
