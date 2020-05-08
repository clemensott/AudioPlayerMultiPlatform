using System;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using System.ComponentModel;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;
using AudioPlayerFrontend.Join;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using StdOttUwp;

namespace AudioPlayerFrontend
{
    class ViewModel : INotifyPropertyChanged
    {
        private ServiceBuild serviceOpenBuild;
        private ServiceBuildResult buildResult;
        private IAudioService audioService;
        private ICommunicator communicator;
        private IServicePlayer servicePlayer;

        public ServiceBuild ServiceOpenBuild
        {
            get => serviceOpenBuild;
            private set
            {
                if (value == serviceOpenBuild) return;

                serviceOpenBuild = value;
                OnPropertyChanged(nameof(ServiceOpenBuild));

                SetBuildResult(serviceOpenBuild);
            }
        }

        public IAudioService AudioService
        {
            get => audioService;
            private set
            {
                if (value == audioService) return;

                audioService = value;
                OnPropertyChanged(nameof(AudioService));
            }
        }

        public ICommunicator Communicator
        {
            get => communicator;
            private set
            {
                if (value == communicator) return;

                communicator = value;
                OnPropertyChanged(nameof(Communicator));

            }
        }

        public IServicePlayer ServicePlayer
        {
            get => servicePlayer;
            private set
            {
                if (value == servicePlayer) return;

                servicePlayer = value;
                OnPropertyChanged(nameof(ServicePlayer));
            }
        }

        public ServiceBuilder Builder { get; }

        public Frame Frame { get; private set; }

        public ViewModel(ServiceBuilder builder)
        {
            Builder = builder;
        }

        public Task ConnectAsync()
        {
            ServiceOpenBuild = Communicator != null ? Open() : Build();
            Frame.Navigate(typeof(BuildOpenPage), ServiceOpenBuild);

            return ServiceOpenBuild.CompleteToken.EndTask;
        }

        public Task ConnectAsync(Frame frame)
        {
            Frame = frame;

            return ConnectAsync();
        }

        private ServiceBuild Build()
        {
            return ServiceBuild.Build(Builder, TimeSpan.FromMilliseconds(200), AudioServiceHelper.Current);
        }

        private ServiceBuild Open()
        {
            return ServiceBuild.Open(Communicator, AudioService,
                ServicePlayer, buildResult.Data, TimeSpan.FromMilliseconds(200));
        }

        private async void SetBuildResult(ServiceBuild build)
        {
            if (build == null) return;

            if (Communicator != null) Communicator.Disconnected -= Communicator_Disconnected;

            ServiceBuildResult result = await build.CompleteToken.ResultTask;

            if (build != ServiceOpenBuild) return;

            AudioService = result?.AudioService;
            Communicator = result?.Communicator;
            ServicePlayer = result?.ServicePlayer;

            buildResult = result;

            if (Communicator != null) Communicator.Disconnected += Communicator_Disconnected;
        }

        private async void Communicator_Disconnected(object sender, DisconnectedEventArgs e)
        {
            if (e.OnDisconnect) return;

            await UwpUtils.RunSafe(async () =>
            {
                await CloseAsync();
                if (Frame.CurrentSourcePageType == typeof(BuildOpenPage)) Frame.GoBack();

                await ConnectAsync();
            });
        }

        public async Task CloseAsync()
        {
            ServiceOpenBuild?.Cancel();
            ServiceOpenBuild = null;
            await (Communicator?.CloseAsync() ?? Task.CompletedTask);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
