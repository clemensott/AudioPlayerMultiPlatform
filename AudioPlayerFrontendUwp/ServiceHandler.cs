using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Player;
using AudioPlayerFrontend.Join;
using StdOttStandard.Dispatch;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerFrontend
{
    class ServiceHandler : INotifyPropertyChanged
    {
        private const string dataFileName = "data.xml";

        private readonly Dispatcher dispatcher;
        private bool isClient;
        private ServiceBuild serviceOpenBuild;
        private ServiceBuildResult buildResult;
        private IAudioService audioService;
        private ICommunicator communicator;
        private IServicePlayer servicePlayer;

        public bool IsClient
        {
            get => isClient;
            set
            {
                if (value == isClient) return;

                isClient = value;
                OnPropertyChanged(nameof(IsClient));
            }
        }

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

        public IAudioService Audio
        {
            get => audioService;
            private set
            {
                if (value == audioService) return;

                audioService = value;
                OnPropertyChanged(nameof(Audio));
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

                IsClient = value is IClientCommunicator;
            }
        }

        public IServicePlayer Player
        {
            get => servicePlayer;
            private set
            {
                if (value == servicePlayer) return;

                servicePlayer = value;
                OnPropertyChanged(nameof(Player));
            }
        }

        public ServiceBuilder Builder { get; }

        public ServiceHandler(Dispatcher dispatcher, ServiceBuilder builder)
        {
            this.dispatcher = dispatcher;
            Builder = builder;
        }

        public Task ConnectAsync()
        {
            ServiceOpenBuild = new ServiceBuild();

            return dispatcher.Run(() =>
            {
                ServiceBuild build = ServiceOpenBuild;
                if (build == null) return Task.CompletedTask;

                Builder.DataFilePath = Builder.BuildClient ? null : dataFileName;

                ICommunicator communicator = Communicator;
                if (communicator == null) build.StartBuild(Builder, TimeSpan.FromMilliseconds(200));
                else build.StartOpen(communicator, Audio, Player, buildResult.Data, TimeSpan.FromMilliseconds(200));

                return ServiceOpenBuild.CompleteToken.EndTask;
            });
        }

        private async void SetBuildResult(ServiceBuild build)
        {
            if (build == null) return;

            if (Communicator != null) Communicator.Disconnected -= Communicator_Disconnected;

            Player oldPlayer = Player?.Player as Player;
            ServiceBuildResult result = await build.CompleteToken.ResultTask;

            if (build != ServiceOpenBuild) return;

            Audio = result?.AudioService;
            Communicator = result?.Communicator;
            Player = result?.ServicePlayer;

            buildResult = result;

            if (Communicator != null) Communicator.Disconnected += Communicator_Disconnected;

            if (oldPlayer != null)
            {
                oldPlayer.NextPressed += Player_NextPressed;
                oldPlayer.PreviousPressed += Player_PreviousPressed;
            }

            Player newPlayer = Player?.Player as Player;
            if (newPlayer != null)
            {
                newPlayer.NextPressed += Player_NextPressed;
                newPlayer.PreviousPressed += Player_PreviousPressed;
            }
        }

        private void Player_NextPressed(object sender, HandledEventArgs e)
        {
            Audio?.SetNextSong();
            e.Handled = true;
        }

        private void Player_PreviousPressed(object sender, HandledEventArgs e)
        {
            Audio?.SetPreviousSong();
            e.Handled = true;
        }

        private async void Communicator_Disconnected(object sender, DisconnectedEventArgs e)
        {
            if (e.OnDisconnect) return;

            await CloseAsync();
            await ConnectAsync();
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
