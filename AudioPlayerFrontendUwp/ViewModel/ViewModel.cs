using AudioPlayerBackend;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private bool isTryOpening, viewAdvancedSettings;
        private AudioViewModel audioService;

        public bool IsTryOpening
        {
            get { return isTryOpening; }
            set
            {
                if (value == isTryOpening) return;

                isTryOpening = value;
                OnPropertyChanged(nameof(IsTryOpening));
            }
        }

        public bool ViewAdvancedSettings
        {
            get { return viewAdvancedSettings; }
            set
            {
                if (value == viewAdvancedSettings) return;

                viewAdvancedSettings = value;
                OnPropertyChanged(nameof(ViewAdvancedSettings));
            }
        }

        public AudioViewModel AudioService
        {
            get { return audioService; }
            set
            {
                if (value == audioService) return;

                audioService = value;
                OnPropertyChanged(nameof(AudioService));
            }
        }

        public ServiceBuilder Builder { get; private set; }

        public ViewModel(ServiceBuilder builder)
        {
            Builder = builder;
        }

        public async Task<bool> BuildAsync()
        {
            IsTryOpening = true;

            while (true)
            {
                try
                {
                    AudioService = new AudioViewModel(await Builder.Build());
                    break;
                }
                catch
                {
                    await Task.Delay(200);

                    if (IsTryOpening) continue;

                    AudioService = null;
                    break;
                }
            }

            IsTryOpening = false;

            return AudioService != null;
        }

        public async Task<bool> OpenAsync(IMqttAudio mqttAudio)
        {
            if (mqttAudio == null || mqttAudio.IsOpen) return true;

            IsTryOpening = true;

            while (true)
            {
                try
                {
                    await mqttAudio.OpenAsync();
                    break;
                }
                catch
                {
                    await Task.Delay(200);

                    if (IsTryOpening) continue;

                    break;
                }
            }

            IsTryOpening = false;

            return mqttAudio.IsOpen;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
