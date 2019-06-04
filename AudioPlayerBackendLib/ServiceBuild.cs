using System;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Player;

namespace AudioPlayerBackend
{
    public class ServiceBuild
    {
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
                    await await Task.WhenAny(communicator.OpenAsync(CommunicatorToken), CommunicatorToken.EndTask);

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

            while (true)
            {
                try
                {
                    service = serviceBuilder.Service ?? new AudioService(serviceHelper);
                    await await Task.WhenAny(communicator.SetService(service, SyncToken), SyncToken.EndTask);

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
                    await Task.WhenAny(communicator.OpenAsync(CommunicatorToken), CommunicatorToken.EndTask);

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

            while (true)
            {
                try
                {
                    await Task.WhenAny(communicator.SyncService(SyncToken), SyncToken.EndTask);

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

            PlayerToken.Successful(player);
            CompleteToken.Successful(new ServiceBuildResult(service, communicator, player));
        }
    }
}
