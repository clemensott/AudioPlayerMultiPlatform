using AudioPlayerBackend.AudioLibrary.LibraryRepo;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;
using StdOttStandard;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerFrontend
{
    class MemoryHandler
    {
        private static readonly TimeSpan inativeTimeBackground = TimeSpan.FromMinutes(30),
            inativeTimeForeground = TimeSpan.FromMinutes(0.1);

        private readonly App app;
        private readonly ResetTimer closeBackgroundTimer, closeForegroundTimer;
        private bool isStarted, isInBackground;
        private ILibraryRepo libraryRepo;
        private PlaybackState playState;

        public MemoryHandler(App app)
        {
            this.app = app;

            closeBackgroundTimer = new ResetTimer(inativeTimeBackground);
            closeBackgroundTimer.RanDown += CloseBackgroundTimer_RanDown;

            closeForegroundTimer = new ResetTimer(inativeTimeForeground);
            closeForegroundTimer.RanDown += CloseForegroundTimer_RanDown;
        }

        private void CloseBackgroundTimer_RanDown(object sender, EventArgs e)
        {
            if (isInBackground && playState == PlaybackState.Paused) app.BackgroundTaskHandler.Stop();
        }

        private void CloseForegroundTimer_RanDown(object sender, EventArgs e)
        {
            if (!isInBackground) return;

            app.ForegroundTaskHandler.Stop();

            if (!app.AudioServicesHandler.AudioServices.BackgroundServices.Any()) app.BackgroundTaskHandler.Stop();
        }

        public void Start()
        {
            if (isStarted) return;
            isStarted = true;

            app.EnteredBackground += OnEnteredBackground;
            app.LeavingBackground += OnLeavingBackground;

            app.AudioServicesHandler.AddAudioServicesChangedListener(ServicesHandler_AudioServicesChanged);
        }

        private void UnsubscribeServicesHandler()
        {
            app.AudioServicesHandler.AudioServicesChanged -= ServicesHandler_AudioServicesChanged;
        }

        private async void ServicesHandler_AudioServicesChanged(object sender, AudioServicesChangedEventArgs e)
        {
            await HandleAudioServices(e.NewServices);
        }

        private async Task HandleAudioServices(AudioServices audioServices)
        {
            UnsubscribeLibraryRepo();
            playState = PlaybackState.Paused;

            if (audioServices == null) return;

            libraryRepo = audioServices?.GetLibraryRepo();

            SubscribeLibraryRepo();

            closeBackgroundTimer.TriggerReset();
            closeForegroundTimer.TriggerReset();

            Library library = await libraryRepo.GetLibrary();
            playState = library.PlayState;
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

            app.AudioServicesHandler.AudioServices?.StopIntensiveServices();
        }

        private async void OnLeavingBackground(object sender, object e)
        {
            isInBackground = false;

            closeBackgroundTimer.Cancel();
            closeForegroundTimer.Cancel();

            if (!app.AudioServicesHandler.IsStarted) app.AudioServicesHandler.Start();
            app.AudioServicesHandler.AudioServices?.StartIntensiveServices();

            if (!app.BackgroundTaskHandler.IsRunning) await app.BackgroundTaskHelper.Start();
            app.ForegroundTaskHandler.Start();
        }

        public void Stop()
        {
            isStarted = false;

            closeBackgroundTimer.Cancel();
            closeForegroundTimer.Cancel();

            app.BackgroundTaskHandler.Stop();
            app.ForegroundTaskHandler.Stop();
        }
    }
}
