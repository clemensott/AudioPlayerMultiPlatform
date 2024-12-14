using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;
using AudioPlayerFrontend.Background;
using StdOttStandard;
using System;
using Windows.UI.Xaml;

namespace AudioPlayerFrontend
{
    class MemoryHandler
    {
        private static readonly TimeSpan inativeTimeBackground = TimeSpan.FromMinutes(30),
            inativeTimeForeground = TimeSpan.FromMinutes(0.1);

        private readonly Application application;
        private readonly AudioServicesHandler servicesHandler;
        private readonly BackgroundTaskHandler backgroundTaskHandler;
        private readonly ForegroundTaskHandler foregroundTaskHandler;
        private readonly ResetTimer closeBackgroundTimer, closeForegroundTimer;
        private bool isStarted, isInBackground;
        private ILibraryRepo libraryRepo;
        private PlaybackState playState;

        public MemoryHandler(Application application, AudioServicesHandler servicesHandler,
            BackgroundTaskHandler backgroundTaskHandler, ForegroundTaskHandler foregroundTaskHandler)
        {
            this.application = application;
            this.servicesHandler = servicesHandler;
            this.backgroundTaskHandler = backgroundTaskHandler;
            this.foregroundTaskHandler = foregroundTaskHandler;

            closeBackgroundTimer = new ResetTimer(inativeTimeBackground);
            closeBackgroundTimer.RanDown += CloseBackgroundTimer_RanDown;

            closeForegroundTimer = new ResetTimer(inativeTimeForeground);
            closeForegroundTimer.RanDown += CloseForegroundTimer_RanDown;
        }

        private void CloseBackgroundTimer_RanDown(object sender, EventArgs e)
        {
            if (isInBackground && playState == PlaybackState.Paused) backgroundTaskHandler.Stop();
        }

        private void CloseForegroundTimer_RanDown(object sender, EventArgs e)
        {
            if (isInBackground) foregroundTaskHandler.Stop();
        }

        public void Start()
        {
            if (isStarted) return;
            isStarted = true;

            application.EnteredBackground += OnEnteredBackground;
            application.LeavingBackground += OnLeavingBackground;

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

            closeBackgroundTimer.TriggerReset();
            closeForegroundTimer.TriggerReset();
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

            if (e.NewValue == PlaybackState.Paused) closeBackgroundTimer.TriggerReset();
            else closeBackgroundTimer.Cancel();
        }

        private void OnEnteredBackground(object sender, object e)
        {
            isInBackground = true;

            closeBackgroundTimer.TriggerReset();
            closeForegroundTimer.TriggerReset();

            servicesHandler.AudioServices?.StopIntensiveServices();
        }

        private void OnLeavingBackground(object sender, object e)
        {
            isInBackground = false;

            closeBackgroundTimer.Cancel();
            closeForegroundTimer.Cancel();

            servicesHandler.AudioServices?.StartIntensiveServices();
        }

        public void Stop()
        {
            isStarted = false;

            closeBackgroundTimer.Cancel();
            closeForegroundTimer.Cancel();

            backgroundTaskHandler.Stop();
            foregroundTaskHandler.Stop();
        }
    }
}
