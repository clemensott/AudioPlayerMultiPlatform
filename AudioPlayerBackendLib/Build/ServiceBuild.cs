using System;
using System.ComponentModel;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Data;
using AudioPlayerBackend.Player;

namespace AudioPlayerBackend.Build
{
    public enum BuildState { Init, OpenCommunicator, SyncCommunicator, SendCommands, CreatePlayer, CompleteSerivce, Finished }

    public class ServiceBuild : INotifyPropertyChanged
    {
        private bool sendCommandsDirect, sendToggle;
        private int songOffset;
        private BuildState state;
        private PlaybackState? sendPlayState;
        private ICommunicator communicator;
        private IAudioService service;
        private IServicePlayer servicePlayer;

        public bool SendToggle
        {
            get => sendToggle;
            private set
            {
                if (value == sendToggle) return;

                sendToggle = value;
                OnPropertyChanged(nameof(SendToggle));
            }
        }

        public int SongOffset
        {
            get => songOffset;
            private set
            {
                if (value == songOffset) return;

                songOffset = value;
                OnPropertyChanged(nameof(SongOffset));
            }
        }

        public PlaybackState? SendPlayState
        {
            get => sendPlayState;
            private set
            {
                if (value == sendPlayState) return;

                sendPlayState = value;
                OnPropertyChanged(nameof(SendPlayState));
            }
        }

        public BuildState State
        {
            get => state;
            set
            {
                if (value == state) return;

                state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public ICommunicator Communicator
        {
            get => communicator;
            set
            {
                if (value == communicator) return;

                communicator = value;
                OnPropertyChanged(nameof(Communicator));
            }
        }

        public IAudioService Service
        {
            get => service;
            set
            {
                if (value == service) return;

                service = value;
                OnPropertyChanged(nameof(Service));
            }
        }

        public IServicePlayer ServicePlayer
        {
            get => servicePlayer;
            set
            {
                if (value == servicePlayer) return;

                servicePlayer = value;
                OnPropertyChanged(nameof(ServicePlayer));
            }
        }

        public BuildStatusToken<ICommunicator> CommunicatorToken { get; }

        public BuildStatusToken<IAudioService> SyncToken { get; }

        public BuildStatusToken<IServicePlayer> PlayerToken { get; }

        public BuildStatusToken<ServiceBuildResult> CompleteToken { get; }

        public ServiceBuild()
        {
            State = BuildState.Init;
            CommunicatorToken = new BuildStatusToken<ICommunicator>();
            SyncToken = new BuildStatusToken<IAudioService>();
            PlayerToken = new BuildStatusToken<IServicePlayer>();
            CompleteToken = new BuildStatusToken<ServiceBuildResult>();
        }

        public void Cancel()
        {
            CommunicatorToken.Cancel();
            SyncToken.Cancel();
            PlayerToken.Cancel();
            CompleteToken.Cancel();
        }

        public void Settings()
        {
            CommunicatorToken.Settings();
            SyncToken.Settings();
            PlayerToken.Settings();
            CompleteToken.Settings();
        }

        public static ServiceBuild Build(ServiceBuilder serviceBuilder, TimeSpan delayTime)
        {
            ServiceBuild build = new ServiceBuild();
            build.StartBuild(serviceBuilder, delayTime);

            return build;
        }

        public async void StartBuild(ServiceBuilder serviceBuilder, TimeSpan delayTime)
        {
            if (State != BuildState.Init) throw new InvalidOperationException("Build has already benn started: " + State);

            try
            {
                while (true)
                {
                    CommunicatorToken.Reset();
                    SyncToken.Reset();
                    PlayerToken.Reset();
                    CompleteToken.Reset();

                    try
                    {
                        State = BuildState.OpenCommunicator;
                        if (Communicator == null) Communicator = serviceBuilder.CreateCommunicator();

                        if (Communicator != null && !Communicator.IsOpen)
                        {
                            await await Task.WhenAny(Communicator.OpenAsync(CommunicatorToken), CommunicatorToken.EndTask);
                        }

                        if (CommunicatorToken.IsEnded.HasValue) return;

                        CommunicatorToken.End(BuildEndedType.Successful, Communicator);
                    }
                    catch (Exception e)
                    {
                        CommunicatorToken.Exception = e;

                        if (CommunicatorToken.IsEnded.HasValue) return;

                        await Task.Delay(delayTime);
                        continue;
                    }

                    Task sendCmdTask = SendCommands();

                    try
                    {
                        SyncToken.Reset();
                        State = BuildState.SyncCommunicator;
                        service = new AudioService(serviceBuilder.NotifyPropertyChangedHelper);

                        if (Communicator != null)
                        {
                            await await Task.WhenAny(Communicator.SetService(service, SyncToken), SyncToken.EndTask);
                        }

                        if (SyncToken.IsEnded.HasValue) return;

                        SyncToken.End(BuildEndedType.Successful, service);
                    }
                    catch (Exception e)
                    {
                        SyncToken.Exception = e;

                        if (SyncToken.IsEnded.HasValue) return;

                        await Task.Delay(delayTime);
                        continue;
                    }

                    try
                    {
                        State = BuildState.SendCommands;
                        await sendCmdTask;
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                    }

                    try
                    {
                        PlayerToken.Reset();
                        State = BuildState.CreatePlayer;
                        servicePlayer = serviceBuilder.CreateServicePlayer(service);

                        if (PlayerToken.IsEnded.HasValue) return;

                        PlayerToken.End(BuildEndedType.Successful, servicePlayer);
                    }
                    catch (Exception e)
                    {
                        PlayerToken.Exception = e;

                        if (PlayerToken.IsEnded.HasValue) return;

                        await Task.Delay(delayTime);
                        continue;
                    }

                    try
                    {
                        State = BuildState.CompleteSerivce;
                        ReadWriteAudioServiceData data = serviceBuilder.CompleteService(service);

                        if (CompleteToken.IsEnded.HasValue) return;

                        ServiceBuildResult result = new ServiceBuildResult(service, Communicator, servicePlayer, data);
                        CompleteToken.End(BuildEndedType.Successful, result);
                    }
                    catch (Exception e)
                    {
                        CompleteToken.Exception = e;

                        if (CompleteToken.IsEnded.HasValue) return;

                        await Task.Delay(delayTime);
                        continue;
                    }
                    break;
                }

                State = BuildState.Finished;
            }
            finally
            {
                if (Communicator != null && State != BuildState.Finished) await Communicator.CloseAsync();
            }
        }

        public static ServiceBuild Open(ICommunicator communicator, IAudioService service,
            IServicePlayer player, ReadWriteAudioServiceData data, TimeSpan delayTime)
        {
            ServiceBuild build = new ServiceBuild();
            build.StartOpen(communicator, service, player, data, delayTime);

            return build;
        }

        public async void StartOpen(ICommunicator communicator, IAudioService service,
            IServicePlayer player, ReadWriteAudioServiceData data, TimeSpan delayTime)
        {
            if (State != BuildState.Init) throw new InvalidOperationException("Build has already benn started: " + State);

            try
            {
                Communicator = communicator;
                Service = service;

                while (true)
                {
                    try
                    {
                        CommunicatorToken.Reset();
                        State = BuildState.OpenCommunicator;

                        if (Communicator != null)
                        {
                            await await Task.WhenAny(Communicator.OpenAsync(CommunicatorToken), CommunicatorToken.EndTask);
                        }

                        if (CommunicatorToken.IsEnded.HasValue) return;

                        CommunicatorToken.End(BuildEndedType.Successful, Communicator);
                    }
                    catch (Exception e)
                    {
                        CommunicatorToken.Exception = e;

                        if (CommunicatorToken.IsEnded.HasValue) return;

                        await Task.Delay(delayTime);
                        continue;
                    }

                    Task sendCmdTask = SendCommands();

                    try
                    {
                        SyncToken.Reset();
                        State = BuildState.SyncCommunicator;

                        if (Communicator != null)
                        {
                            await await Task.WhenAny(Communicator.SyncService(SyncToken), SyncToken.EndTask);
                        }

                        if (SyncToken.IsEnded.HasValue) return;

                        SyncToken.End(BuildEndedType.Successful, service);
                    }
                    catch (Exception e)
                    {
                        SyncToken.Exception = e;

                        if (SyncToken.IsEnded.HasValue) return;

                        await Task.Delay(delayTime);
                        continue;
                    }

                    try
                    {
                        State = BuildState.SendCommands;
                        await sendCmdTask;
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                    }
                    break;
                }

                State = BuildState.CreatePlayer;
                PlayerToken.Successful(player);
                State = BuildState.CompleteSerivce;
                CompleteToken.Successful(new ServiceBuildResult(service, Communicator, player, data));
                State = BuildState.Finished;
            }
            finally
            {
                if (Communicator != null && State != BuildState.Finished) await Communicator.CloseAsync();
            }
        }

        private async Task SendCommands()
        {
            sendCommandsDirect = true;

            if (Communicator == null) return;

            while (SongOffset < 0)
            {
                SongOffset++;
                await Communicator.PreviousSong();
            }

            while (SongOffset > 0)
            {
                SongOffset--;
                await Communicator.NextSong();
            }

            if (SendPlayState == PlaybackState.Playing) await Communicator.PlaySong();
            else if (SendPlayState == PlaybackState.Paused) await Communicator.PauseSong();
            else if (SendPlayState == PlaybackState.Stopped) await Communicator.StopSong();
            else if (SendToggle)
            {
                await Communicator.ToggleSong();
                SendToggle = false;
            }
        }

        public async Task SetNextSong(int offset = 1)
        {
            if (sendCommandsDirect)
            {
                for (int i = 0; i < offset; i++)
                {
                    await Communicator.NextSong();
                }
            }
            else SongOffset += offset;
        }

        public async Task SetPreviousSong(int offset = 1)
        {
            if (sendCommandsDirect)
            {
                for (int i = 0; i < offset; i++)
                {
                    await Communicator.PreviousSong();
                }
            }
            else SongOffset -= offset;
        }

        public async Task SetPlayState(PlaybackState? state)
        {
            SendToggle = false;
            SendPlayState = state;

            if (sendCommandsDirect)
            {
                if (state == PlaybackState.Playing) await Communicator.PlaySong();
                else if (state == PlaybackState.Paused) await Communicator.PauseSong();
                else if (state == PlaybackState.Stopped) await Communicator.StopSong();
            }
        }

        public async Task SetToggle(bool value = true)
        {
            SendToggle = value;
            SendPlayState = null;

            if (sendCommandsDirect)
            {
                if (SendToggle)
                {
                    await Communicator.ToggleSong();
                    SendToggle = false;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
