using AudioPlayerBackend.Build;
using AudioPlayerBackendUwpLib;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace AudioPlayerFrontend.Background
{
    class BackgroundTaskHelper
    {
        private const string applicationBackgroundTaskBuilderName = "AppRemoteAudioPlayerTask";

        private readonly AudioServicesHandler audioServicesHandler;
        private ApplicationTrigger appTrigger;

        public BackgroundTaskHelper(AudioServicesHandler audioServicesHandler)
        {
            this.audioServicesHandler = audioServicesHandler;
        }

        public async Task Start()
        {
            if (appTrigger == null) RegisterAppBackgroundTask();

            await appTrigger.RequestAsync();
        }

        private void RegisterAppBackgroundTask()
        {
            IBackgroundTaskRegistration taskRegistration;
            Guid taskRegistrationId = Settings.Current.ApplicationBackgroundTaskRegistrationId;
            if (BackgroundTaskRegistration.AllTasks.TryGetValue(taskRegistrationId, out taskRegistration))
            {
                if (taskRegistration is BackgroundTaskRegistration lastTaskRegistration &&
                    lastTaskRegistration.Trigger is ApplicationTrigger)
                {
                    appTrigger = (ApplicationTrigger)lastTaskRegistration.Trigger;
                    lastTaskRegistration.Progress += TaskRegistration_Progress;
                    return;
                }

                taskRegistration.Unregister(false);
            }

            appTrigger = new ApplicationTrigger();

            BackgroundTaskBuilder builder = new BackgroundTaskBuilder
            {
                Name = applicationBackgroundTaskBuilderName,
            };

            builder.SetTrigger(appTrigger);

            taskRegistration = builder.Register();            
            Settings.Current.ApplicationBackgroundTaskRegistrationId = taskRegistration.TaskId;

            taskRegistration.Progress += TaskRegistration_Progress;
        }

        private void TaskRegistration_Progress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            audioServicesHandler.Start();
        }
    }
}
