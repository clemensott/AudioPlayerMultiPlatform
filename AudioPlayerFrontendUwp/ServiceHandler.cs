using AudioPlayerBackend;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Data;
using AudioPlayerBackend.Player;
using AudioPlayerFrontend.Extensions;
using AudioPlayerFrontend.Join;
using StdOttStandard.Dispatch;
using StdOttStandard.TaskCompletionSources;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AudioPlayerFrontend
{
    class ServiceHandler : INotifyPropertyChanged, IDisposable
    {
        private const string dataFileName = "data.json";

        private readonly IInvokeDispatcherService dispatcher;
        private readonly Dispatcher backgrounTaskDispatcher;
        private readonly SemaphoreSlim keepOpenSem;
        private bool keepService;
        private Frame frame;
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
            keepOpenSem = new SemaphoreSlim(0);
            ViewModel = viewModel;
        }

        public async Task Start(Frame frame)
        {
            this.frame = frame;

            Application.Current.EnteredBackground += Application_EnteredBackground;
            Application.Current.LeavingBackground += Application_LeavingBackground;

            await Rebuild(true);
        }

        public async Task Rebuild(bool forceBuild)
        {
            await CloseAsync();
            keepOpenSem.Release();
            StartKeepService(forceBuild);
        }

        private async void StartKeepService(bool forceBuild)
        {
            if (keepService) return;
            keepService = true;

            try
            {
                while (keepService)
                {
                    await keepOpenSem.WaitAsync();

                    bool wasOnOpenPage = frame.CurrentSourcePageType == typeof(BuildOpenPage);
                    if (!wasOnOpenPage) frame.NavigateToBuildOpenPage(this);

                    if (Communicator != null) Communicator.Disconnected -= Communicator_Disconnected;

                    ServiceBuild build = ServiceOpenBuild = new ServiceBuild();
                    BuildStatusToken<ServiceBuildResult> completeToken = await backgrounTaskDispatcher.Run(() =>
                    {
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

                        return build.CompleteToken;
                    });

                    ServiceBuildResult result = await completeToken.ResultTask;
                    BuildEndedType endedType = await completeToken.EndTask;

                    switch (endedType)
                    {
                        case BuildEndedType.Successful:
                            SetBuildResult(result);

                            if (wasOnOpenPage)
                            {
                                frame.NavigateToMainPage(this);
                                frame.BackStack.RemoveAt(0);
                            }
                            else if (frame.CanGoBack) frame.GoBack();
                            break;

                        case BuildEndedType.Canceled:
                            // canceled means in the uwp app, a new attempt was started
                            break;

                        case BuildEndedType.Settings:
                            TaskCompletionSourceS<ServiceBuilder> settingsResult = new TaskCompletionSourceS<ServiceBuilder>(Builder.Clone());
                            frame.NavigateToSettingsPage(settingsResult);

                            ServiceBuilder newBuilder = await settingsResult.Task;

                            if (newBuilder != null) Builder = newBuilder;
                            forceBuild = true;
                            break;
                    }
                }
            }
            finally
            {
                keepService = false;
            }
        }

        private async void Application_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            if (ServiceOpenBuild?.CompleteToken.IsEnded.HasValue != false) return;

            Deferral deferral = e.GetDeferral();
            try
            {
                await CloseAsync();
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void Application_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            if (ServiceOpenBuild == null  || ServiceOpenBuild.CompleteToken.IsEnded == BuildEndedType.Canceled) await Rebuild(true);
        }

        private void SetBuildResult(ServiceBuildResult result)
        {
            IPlayer oldPlayer = buildResult?.ServicePlayer?.Player;

            buildResult = result;
            OnPropertyChanged(nameof(Audio));
            OnPropertyChanged(nameof(Communicator));
            OnPropertyChanged(nameof(ServicePlayer));
            OnPropertyChanged(nameof(Data));

            ViewModel.Audio = Audio;
            ViewModel.IsClient = Communicator is IClientCommunicator;

            if (Communicator != null) Communicator.Disconnected += Communicator_Disconnected;

            Unsubscribe(oldPlayer as Player);
            Subscribe(ServicePlayer?.Player as Player);
        }

        private void Subscribe(Player player)
        {
            if (player == null) return;

            player.NextPressed += Player_NextPressed;
            player.PreviousPressed += Player_PreviousPressed;
            player.PlayStateChanged += Player_PlayStateChanged;
        }

        private void Unsubscribe(Player player)
        {
            if (player == null) return;

            player.NextPressed -= Player_NextPressed;
            player.PreviousPressed -= Player_PreviousPressed;
            player.PlayStateChanged -= Player_PlayStateChanged;
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

            await frame.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => Rebuild(false));
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
            dispatcher.InvokeDispatcher(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)));
        }

        public void Dispose()
        {
            Communicator?.Dispose();
            ServicePlayer?.Dispose();
            Data?.Dispose();

            buildResult = null;
        }
    }
}
