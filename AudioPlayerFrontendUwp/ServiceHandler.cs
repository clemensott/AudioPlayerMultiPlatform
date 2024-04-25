using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Data;
using AudioPlayerBackend.Player;
using AudioPlayerFrontend.Join;
using StdOttStandard.Dispatch;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AudioPlayerFrontend
{
    class ServiceHandler : INotifyPropertyChanged, IDisposable
    {
        private const string dataFileName = "data.xml";

        private readonly IInvokeDispatcherService dispatcher;
        private readonly Dispatcher backgrounTaskDispatcher;
        private ServiceBuilder builder;
        private ServiceBuild serviceOpenBuild;
        private ServiceBuildResult buildResult;

        public ServiceBuilder Builder
        {
            get => builder;
            set
            {
                if (value == builder) return;

                builder = value;
                OnPropertyChanged(nameof(Builder));
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

        public IAudioService Audio => buildResult?.AudioService;

        public ICommunicator Communicator => buildResult?.Communicator;

        public IServicePlayer ServicePlayer => buildResult?.ServicePlayer;

        public ReadWriteAudioServiceData Data => buildResult?.Data;

        public ViewModel ViewModel { get; }

        public ServiceHandler(Dispatcher backgrounTaskDispatcher, ViewModel viewModel)
        {
            dispatcher = AudioPlayerServiceProvider.Current.GetDispatcher();
            this.backgrounTaskDispatcher = backgrounTaskDispatcher;
            ViewModel = viewModel;
        }

        public async Task<ServiceBuildResult> ConnectAsync(bool forceBuild)
        {
            ServiceBuild build = ServiceOpenBuild = new ServiceBuild();

            await backgrounTaskDispatcher.Run(() =>
            {
                if (build != ServiceOpenBuild) return Task.CompletedTask;

                Builder.DataFilePath = Builder.BuildClient ? null : dataFileName;

                ICommunicator communicator = Communicator;
                if (forceBuild || communicator == null)
                {
                    ServicePlayer?.Dispose();
                    Data?.Dispose();
                    build.StartBuild(Builder, TimeSpan.FromMilliseconds(200));
                }
                else
                {
                    build.StartOpen(communicator, Audio, ServicePlayer, Data, TimeSpan.FromMilliseconds(200));
                }

                return build.CompleteToken.EndTask;
            });

            return build == ServiceOpenBuild ? await build.CompleteToken.ResultTask : null;
        }

        private async void SetBuildResult(ServiceBuild build)
        {
            if (Communicator != null) Communicator.Disconnected -= Communicator_Disconnected;

            Player oldPlayer = ServicePlayer?.Player as Player;
            ServiceBuildResult result = await (build?.CompleteToken.ResultTask ?? Task.FromResult<ServiceBuildResult>(null));

            if (build != ServiceOpenBuild) return;

            buildResult = result;
            OnPropertyChanged(nameof(Audio));
            OnPropertyChanged(nameof(Communicator));
            OnPropertyChanged(nameof(ServicePlayer));
            OnPropertyChanged(nameof(Data));

            ViewModel.Audio = Audio;
            ViewModel.IsClient = Communicator is IClientCommunicator;

            if (Communicator != null) Communicator.Disconnected += Communicator_Disconnected;

            Player newPlayer = ServicePlayer?.Player as Player;
            if (oldPlayer != newPlayer)
            {
                if (oldPlayer != null)
                {
                    oldPlayer.NextPressed -= Player_NextPressed;
                    oldPlayer.PreviousPressed -= Player_PreviousPressed;
                    oldPlayer.PlayStateChanged -= Player_PlayStateChanged;
                    await oldPlayer.Stop();
                }

                if (newPlayer != null)
                {
                    newPlayer.NextPressed += Player_NextPressed;
                    newPlayer.PreviousPressed += Player_PreviousPressed;
                    newPlayer.PlayStateChanged += Player_PlayStateChanged;
                }
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

        private void Player_PlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            if (Audio != null) Audio.PlayState = e.NewValue;
        }

        private async void Communicator_Disconnected(object sender, DisconnectedEventArgs e)
        {
            if (e.OnDisconnect) return;

            await CloseAsync();
            await ConnectAsync(false);
        }

        public async Task CloseAsync()
        {
            ServiceOpenBuild?.Cancel();
            ServiceOpenBuild = null;
            await (buildResult?.Communicator?.CloseAsync() ?? Task.CompletedTask);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            dispatcher.InvokeDispatcher(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)));
        }

        public void Dispose()
        {
            buildResult?.Communicator?.Dispose();
            buildResult?.ServicePlayer?.Dispose();
            buildResult?.Data?.Dispose();
        }
    }
}
