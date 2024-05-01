using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using StdOttStandard;
using StdOttStandard.Dispatch;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
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

            Subscribe(service);
            Application.Current.EnteredBackground += OnEnteredBackground;
            Application.Current.LeavingBackground += OnLeavingBackground;
        }

        private void CloseTimer_RanDown(object sender, EventArgs e)
        {
            if (isInBackground && service.Audio?.PlayState != PlaybackState.Playing) Stop();
            else ResetCloseTimer();
        }

        private void Subscribe(ServiceHandler serviceHandler)
        {
            if (serviceHandler == null) return;

            serviceHandler.PropertyChanged += ServiceHandler_PropertyChanged;
            Subscribe(service.Audio);
        }

        private void Unsubscribe(ServiceHandler serviceHandler)
        {
            if (serviceHandler == null) return;

            serviceHandler.PropertyChanged -= ServiceHandler_PropertyChanged;
            Unsubscribe(service.Audio);
        }

        private void ServiceHandler_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ServiceHandler serviceHandler = (ServiceHandler)sender;
            if (e.PropertyName == nameof(serviceHandler.Audio))
            {
                Subscribe(serviceHandler.Audio);
                ResetCloseTimer();
            }
        }

        private void Subscribe(IAudioService audioService)
        {
            if (audioService == null) return;

            audioService.PlayStateChanged += AudioService_PlayStateChanged;
        }

        private void Unsubscribe(IAudioService audioService)
        {
            if (audioService == null) return;

            audioService.PlayStateChanged -= AudioService_PlayStateChanged;
        }

        private void AudioService_PlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            ResetCloseTimer();
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
            Unsubscribe(service);
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
