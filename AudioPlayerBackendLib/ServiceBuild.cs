using System;
using System.ComponentModel;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Player;

namespace AudioPlayerBackend
{
    public class ServiceBuild : INotifyPropertyChanged
    {
        private bool sendCommandsDirect;
        private int songOffset;
        private PlaybackState? sendPlayState;

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

        public BuildStatusToken<ICommunicator> CommunicatorToken { get; }

        public BuildStatusToken<IAudioService> SyncToken { get; }

        public BuildStatusToken<IServicePlayer> PlayerToken { get; }

        public BuildStatusToken<ServiceBuildResult> CompleteToken { get; }

        private ServiceBuild()
        {
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

        public static ServiceBuild Build(ServiceBuilder serviceBuilder, TimeSpan delayTime, IAudioServiceHelper serviceHelper = null)
        {
            ServiceBuild build = new ServiceBuild();
            build.Build(delayTime, serviceBuilder, serviceHelper);

            return build;
        }

        private async void Build(TimeSpan delayTime, ServiceBuilder serviceBuilder, IAudioServiceHelper serviceHelper)
        {
            ICommunicator communicator;
            IAudioService service;
            IServicePlayer servicePlayer;

            while (true)
            {
                try
                {
                    communicator = serviceBuilder.CreateCommunicator();

                    if (communicator != null)
                    {
                        await await Task.WhenAny(communicator.OpenAsync(CommunicatorToken), CommunicatorToken.EndTask);
                    }

                    if (CommunicatorToken.IsEnded.HasValue) return;

                    CommunicatorToken.End(BuildEndedType.Successful, communicator);
                    break;
                }
                catch (Exception e)
                {
                    CommunicatorToken.Exception = e;

                    if (CommunicatorToken.IsEnded.HasValue) return;

                    await Task.Delay(delayTime);
                }
            }

            Task sendCmdTask = SendCommands(communicator);

            while (true)
            {
                try
                {
                    service = serviceBuilder.Service ?? new AudioService(serviceHelper);

                    if (communicator != null)
                    {
                        await await Task.WhenAny(communicator.SetService(service, SyncToken), SyncToken.EndTask);
                    }

                    if (SyncToken.IsEnded.HasValue) return;

                    SyncToken.End(BuildEndedType.Successful, service);
                    break;
                }
                catch (Exception e)
                {
                    SyncToken.Exception = e;

                    if (SyncToken.IsEnded.HasValue) return;

                    await Task.Delay(delayTime);
                }
            }

            try
            {
                await sendCmdTask;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }

            while (true)
            {
                try
                {
                    servicePlayer = serviceBuilder.CreateServicePlayer(service);

                    if (PlayerToken.IsEnded.HasValue) return;

                    PlayerToken.End(BuildEndedType.Successful, servicePlayer);
                    break;
                }
                catch (Exception e)
                {
                    PlayerToken.Exception = e;

                    if (PlayerToken.IsEnded.HasValue) return;

                    await Task.Delay(delayTime);
                }
            }

            while (true)
            {
                try
                {
                    serviceBuilder.CompleteService(service);

                    if (CompleteToken.IsEnded.HasValue) return;

                    ServiceBuildResult result = new ServiceBuildResult(service, communicator, servicePlayer);
                    CompleteToken.End(BuildEndedType.Successful, result);
                    break;
                }
                catch (Exception e)
                {
                    CompleteToken.Exception = e;

                    if (CompleteToken.IsEnded.HasValue) return;

                    await Task.Delay(delayTime);
                }
            }
        }

        public static ServiceBuild Open(ICommunicator communicator, IAudioService service,
            IServicePlayer player, TimeSpan delayTime)
        {
            ServiceBuild build = new ServiceBuild();
            build.Open(delayTime, communicator, service, player);

            return build;
        }

        private async void Open(TimeSpan delayTime, ICommunicator communicator,
            IAudioService service, IServicePlayer player)
        {
            while (true)
            {
                try
                {
                    if (communicator != null)
                    {
                        await Task.WhenAny(communicator.OpenAsync(CommunicatorToken), CommunicatorToken.EndTask);
                    }

                    if (CommunicatorToken.IsEnded.HasValue) return;

                    CommunicatorToken.End(BuildEndedType.Successful, communicator);
                    break;
                }
                catch (Exception e)
                {
                    CommunicatorToken.Exception = e;

                    if (CommunicatorToken.IsEnded.HasValue) return;

                    await Task.Delay(delayTime);
                }
            }

            Task sendCmdTask = SendCommands(communicator);

            while (true)
            {
                try
                {
                    if (communicator != null)
                    {
                        await Task.WhenAny(communicator.SyncService(SyncToken), SyncToken.EndTask);
                    }

                    if (SyncToken.IsEnded.HasValue) return;

                    SyncToken.End(BuildEndedType.Successful, service);
                    break;
                }
                catch (Exception e)
                {
                    SyncToken.Exception = e;

                    if (SyncToken.IsEnded.HasValue) return;

                    await Task.Delay(delayTime);
                }
            }

            try
            {
                await sendCmdTask;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }

            PlayerToken.Successful(player);
            CompleteToken.Successful(new ServiceBuildResult(service, communicator, player));
        }

        private async Task SendCommands(ICommunicator communicator)
        {
            sendCommandsDirect = true;

            if (communicator == null) return;

            while (SongOffset < 0)
            {
                SongOffset++;
                await communicator.PreviousSong();
            }

            while (SongOffset > 0)
            {
                SongOffset--;
                await communicator.NextSong();
            }

            if (SendPlayState == PlaybackState.Playing) await communicator.PlaySong();
            else if (SendPlayState == PlaybackState.Paused) await communicator.PauseSong();
            else if (SendPlayState == PlaybackState.Stopped) await communicator.StopSong();
        }

        public async Task SetNextSong(int offset = 1)
        {
            if (sendCommandsDirect)
            {
                ICommunicator communicator = await CommunicatorToken.ResultTask;

                for (int i = 0; i < offset; i++)
                {
                    await communicator.NextSong();
                }
            }
            else SongOffset += offset;
        }

        public async Task SetPreviousSong(int offset = 1)
        {
            if (sendCommandsDirect)
            {
                ICommunicator communicator = await CommunicatorToken.ResultTask;

                for (int i = 0; i < offset; i++)
                {
                    await communicator.PreviousSong();
                }
            }
            else SongOffset -= offset;
        }

        public async Task SetPlayState(PlaybackState? state)
        {
            SendPlayState = state;

            if (sendCommandsDirect)
            {
                ICommunicator communicator = await CommunicatorToken.ResultTask;

                if (state == PlaybackState.Playing) await communicator.PlaySong();
                else if (state == PlaybackState.Paused) await communicator.PauseSong();
                else if (state == PlaybackState.Stopped) await communicator.StopSong();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
