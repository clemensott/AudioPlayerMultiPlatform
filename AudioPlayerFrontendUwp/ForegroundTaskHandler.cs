using AudioPlayerBackend.Build;
using AudioPlayerFrontend.Extensions;
using StdOttStandard.TaskCompletionSources;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AudioPlayerFrontend
{
    class ForegroundTaskHandler
    {
        private readonly AudioServicesHandler audioServicesHandler;
        private Frame frame;
        private AudioServicesBuilder currentBuilder;

        public ForegroundTaskHandler(AudioServicesHandler audioServicesHandler)
        {
            this.audioServicesHandler = audioServicesHandler;
        }

        public void Start()
        {
            frame = Window.Current.Content as Frame;

            if (frame == null)
            {
                audioServicesHandler.ServicesBuild += AudioServicesHandler_ServicesBuild;
                Window.Current.Content = frame = new Frame();
            }

            if (frame.Content == null)
            {
                if (audioServicesHandler.AudioServices != null)
                {
                    frame.NavigateToMainPage(audioServicesHandler);
                    audioServicesHandler.AudioServices.StartUiServices();
                }
                else
                {
                    frame.NavigateToBuildOpenPage(audioServicesHandler);
                    HandleAudioServiceBuilder(audioServicesHandler.Builder);
                }
            }

            Window.Current.Activate();
        }

        private async void AudioServicesHandler_ServicesBuild(object sender, AudioServicesBuilder e)
        {
            await HandleAudioServiceBuilder(e);
        }

        private async Task HandleAudioServiceBuilder(AudioServicesBuilder builder)
        {
            AudioPlayerBackend.Logs.Log("HandleAudioServiceBuilder3", builder != null, frame != null);
            if (builder == null || frame == null) return;

            await frame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                AudioPlayerBackend.Logs.Log("HandleAudioServiceBuilder4", builder == currentBuilder);
                if (builder == currentBuilder) return;
                currentBuilder = builder;

                bool isOnOpenPage = frame.CurrentSourcePageType == typeof(BuildOpenPage);
                if (!isOnOpenPage) frame.NavigateToBuildOpenPage(audioServicesHandler);

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

                currentBuilder = null;
            });
        }

        public async void Stop()
        {
            audioServicesHandler.ServicesBuild -= AudioServicesHandler_ServicesBuild;
            if (frame == null) return;

            await frame.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                Window.Current.Content = frame = null;

                if (audioServicesHandler.AudioServices != null) await audioServicesHandler.AudioServices.StopUiServices();
            });
        }
    }
}
