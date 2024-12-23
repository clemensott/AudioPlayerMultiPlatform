﻿using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;
using StdOttStandard;
using StdOttStandard.Dispatch;
using System;
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
        private readonly AudioServicesHandler servicesHandler;
        private readonly SemaphoreSlim sem;
        private readonly Dispatcher dispatcher;
        private readonly ResetTimer closeTimer;
        private ILibraryRepo libraryRepo;
        private PlaybackState playState;

        public bool IsRunning { get; private set; }

        public BackgroundTaskHandler(Dispatcher dispatcher, AudioServicesHandler servicesHandler)
        {
            if (Current != null) throw new InvalidOperationException("Only one instance of this class is allowed");
            Current = this;

            isInBackground = false;
            sem = new SemaphoreSlim(0, 1);

            this.dispatcher = dispatcher;
            this.servicesHandler = servicesHandler;

            closeTimer = ResetTimer.Start(inativeTime);
        }

        public void Start()
        {
            ResetCloseTimer();
            closeTimer.RanDown += CloseTimer_RanDown;

            Application.Current.EnteredBackground += OnEnteredBackground;
            Application.Current.LeavingBackground += OnLeavingBackground;

            SubscribeServicesHandler();
        }

        private void CloseTimer_RanDown(object sender, EventArgs e)
        {
            AudioPlayerBackend.Logs.Log("BackgroundTaskHandler.CloseTimer_RanDown", isInBackground, playState);
            if (isInBackground && playState != PlaybackState.Playing) Stop();
            else ResetCloseTimer();
        }

        private void SubscribeServicesHandler()
        {
            servicesHandler.AddAudioServicesChangedListener(ServicesHandler_AudioServicesChanged);
        }

        private void UnsubscribeServicesHandler()
        {
            servicesHandler.AudioServicesChanged -= ServicesHandler_AudioServicesChanged;
        }

        private void ServicesHandler_AudioServicesChanged(object sender, AudioServicesChangedEventArgs e)
        {
            HandleAudioServices(e.NewServices);
        }

        private void HandleAudioServices(AudioServices audioServices)
        {
            UnsubscribeLibraryRepo();

            if (audioServices == null) return;

            libraryRepo = audioServices?.GetLibraryRepo();

            SubscribeLibraryRepo();
            ResetCloseTimer();
        }

        private void SubscribeLibraryRepo()
        {
            if (libraryRepo == null) return;

            libraryRepo.PlayStateChanged += LibraryRepo_PlayStateChanged;
        }

        private void UnsubscribeLibraryRepo()
        {
            if (libraryRepo == null) return;

            libraryRepo.PlayStateChanged -= LibraryRepo_PlayStateChanged;
        }

        private void LibraryRepo_PlayStateChanged(object sender, AudioLibraryChangeArgs<PlaybackState> e)
        {
            playState = e.NewValue;
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

            dispatcher.Start();
            Start();

            await sem.WaitAsync();
            UnsubscribeServicesHandler();
            UnsubscribeLibraryRepo();

            IsRunning = false;

            await dispatcher.Stop();
        }

        private async void ResetCloseTimer()
        {
            await closeTimer.Reset();
        }

        public void Stop()
        {
            AudioPlayerBackend.Logs.Log("BackgroundTaskHandler.Stop", IsRunning, sem.CurrentCount);
            if (IsRunning && sem.CurrentCount == 0) sem.Release();
        }
    }
}
