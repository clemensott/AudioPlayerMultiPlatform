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
        private readonly Dispatcher dispatcher;
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

                ICommunicator communicator = Communicator;
                if (communicator == null) build.StartBuild(Builder, TimeSpan.FromMilliseconds(200), AudioServiceHelper.Current);
                else build.StartOpen(communicator, Audio, Player, buildResult.Data, TimeSpan.FromMilliseconds(200));

                return ServiceOpenBuild.CompleteToken.EndTask;
            });
        }

        private async void SetBuildResult(ServiceBuild build)
        {
            if (build == null) return;

            if (Communicator != null) Communicator.Disconnected -= Communicator_Disconnected;

            ServiceBuildResult result = await build.CompleteToken.ResultTask;

            if (build != ServiceOpenBuild) return;

            Audio = result?.AudioService;
            Communicator = result?.Communicator;
            Player = result?.ServicePlayer;

            buildResult = result;

            if (Communicator != null) Communicator.Disconnected += Communicator_Disconnected;
        }

        public System.Collections.Generic.List<string> disconnectTimes = new System.Collections.Generic.List<string>();
        private async void Communicator_Disconnected(object sender, DisconnectedEventArgs e)
        {
            disconnectTimes.Add($"{DateTime.Now} | {e.OnDisconnect} | {e.Exception?.Message ?? "<no error"}");
            if (e.OnDisconnect) return;

            await CloseAsync();
            await ConnectAsync();
        }

        public System.Collections.Generic.List<DateTime> closeTimes = new System.Collections.Generic.List<DateTime>();
        public async Task CloseAsync()
        {
            closeTimes.Add(DateTime.Now);
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
