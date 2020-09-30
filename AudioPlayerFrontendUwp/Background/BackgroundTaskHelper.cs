using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace AudioPlayerFrontend.Background
{
    class BackgroundTaskHelper
    {
        private const string applicationBackgroundTaskBuilderName = "AppRemoteAudioPlayerTask";

        private static BackgroundTaskHelper instance;

        public static BackgroundTaskHelper Current
        {
            get
            {
                if (instance == null) instance = new BackgroundTaskHelper();

                return instance;
            }
        }

        private ApplicationTrigger appTrigger;

        private BackgroundTaskHelper()
        {
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
                if (taskRegistration is BackgroundTaskRegistration lastTraskRegistration &&
                    lastTraskRegistration.Trigger is ApplicationTrigger)
                {
                    appTrigger = (ApplicationTrigger)lastTraskRegistration.Trigger;
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
        }
    }
}
