using AudioPlayerBackend.Build;
using AudioPlayerFrontend.Extensions;
using StdOttStandard.TaskCompletionSources;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AudioPlayerFrontend
{
    class AudioServicesBuilderNavigationHandler : IDisposable
    {
        private readonly AudioServicesHandler audioServicesHandler;
        private Frame frame;

        public AudioServicesBuilderNavigationHandler(AudioServicesHandler audioServicesHandler)
        {
            this.audioServicesHandler = audioServicesHandler;
        }

        public async void Start(Frame frame)
        {
            this.frame = frame;

            Application.Current.EnteredBackground += Application_EnteredBackground;
            Application.Current.LeavingBackground += Application_LeavingBackground;

            audioServicesHandler.ServicesBuild += AudioServicesHandler_ServicesBuild;
            await HandleAudioServiceBuilder(audioServicesHandler.Builder);
        }

        private async void AudioServicesHandler_ServicesBuild(object sender, AudioServicesBuilder e)
        {
            await HandleAudioServiceBuilder(e);
        }

        private async Task HandleAudioServiceBuilder(AudioServicesBuilder builder)
        {
            if (builder == null || frame == null) return;

            await frame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                bool wasOnOpenPage = frame.CurrentSourcePageType == typeof(BuildOpenPage);
                if (!wasOnOpenPage) frame.NavigateToBuildOpenPage(audioServicesHandler);

                BuildEndedType endedType = await builder.CompleteToken.EndTask;
                switch (endedType)
                {
                    case BuildEndedType.Successful:
                        if (frame.CanGoBack) frame.GoBack();
                        else
                        {
                            frame.NavigateToMainPage(audioServicesHandler);
                            frame.BackStack.RemoveAt(0);
                        }
                        break;

                    case BuildEndedType.Canceled:
                        // canceled means in the uwp app, a new attempt was started
                        break;

                    case BuildEndedType.Settings:
                        await audioServicesHandler.Stop();

                        TaskCompletionSourceS<AudioServicesBuildConfig> settingsResult =
                            new TaskCompletionSourceS<AudioServicesBuildConfig>(audioServicesHandler.Config.Clone());
                        frame.NavigateToSettingsPage(settingsResult);

                        AudioServicesBuildConfig newConfig = await settingsResult.Task;

                        audioServicesHandler.Start(newConfig ?? audioServicesHandler.Config);
                        break;
                }
            });
        }

        private async void Application_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            if (audioServicesHandler.AudioServices != null) return;

            Deferral deferral = e.GetDeferral();
            try
            {
                await audioServicesHandler.Stop();
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void Application_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            if (!audioServicesHandler.IsStarted) audioServicesHandler.Start();
        }

        public void Dispose()
        {
            Application.Current.EnteredBackground -= Application_EnteredBackground;
            Application.Current.LeavingBackground -= Application_LeavingBackground;

            audioServicesHandler.ServicesBuild -= AudioServicesHandler_ServicesBuild;
        }
    }
}
