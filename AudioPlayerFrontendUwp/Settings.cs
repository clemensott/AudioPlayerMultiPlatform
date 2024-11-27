using StdOttUwp.ApplicationDataObjects;
using System;
using Windows.Storage;

namespace AudioPlayerFrontend
{
    class Settings : AppDataContainerObject
    {
        private static Settings instance;

        public static Settings Current
        {
            get
            {
                if (instance == null) instance = new Settings();

                return instance;
            }
        }

        public int BackgroundTaskPort
        {
            get => GetValue(nameof(BackgroundTaskPort), 28171);
            set => SetValue(nameof(BackgroundTaskPort), value);
        }

        public Guid ApplicationBackgroundTaskRegistrationId
        {
            get
            {
                string idText;
                Guid id;
                if (TryGetValue(nameof(ApplicationBackgroundTaskRegistrationId), out idText) &&
                    Guid.TryParse(idText, out id)) return id;

                return Guid.Empty;
            }
            set => SetValue(nameof(ApplicationBackgroundTaskRegistrationId), value.ToString());
        }

        public string UnhandledExceptionText
        {
            get => GetValue<string>(nameof(UnhandledExceptionText));
        }

        public DateTime UnhandledExceptionTime
        {
            get => new DateTime(GetValue<long>(nameof(UnhandledExceptionTime)));
        }

        public DateTime SuspendTime
        {
            get => new DateTime(GetValue<long>(nameof(SuspendTime)));
            set => SetValue(nameof(SuspendTime), value.Ticks);
        }

        private Settings() : base(ApplicationData.Current.LocalSettings)
        {
        }

        public void SetUnhandledException(Exception e)
        {
            if (SetValue(nameof(UnhandledExceptionText), e.ToString()))
            {
                SetValue(nameof(UnhandledExceptionTime), DateTime.Now.Ticks);
            }
        }
    }
}
