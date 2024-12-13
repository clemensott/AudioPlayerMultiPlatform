using AudioPlayerBackend.Build.Repo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.OwnTcp;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Build.Repo.OwnTcp
{
    internal class OwnTcpServerAudioServicesRepoConnector : OwnTcpBaseServerConnector, IServerAudioServicesRepoConnector
    {
        private readonly IAudioServicesRepo audioServicesRepo;
        public OwnTcpServerAudioServicesRepoConnector(IAudioServicesRepo audioServicesRepo, IServerCommunicator serverCommunicator)
            : base(nameof(IAudioServicesRepo), serverCommunicator)
        {
            this.audioServicesRepo = audioServicesRepo;
        }

        protected override Task<byte[]> OnMessageReceived(string subTopic, byte[] payload)
        {
            ByteQueue queue = payload;
            switch (subTopic)
            {
                case nameof(audioServicesRepo.TriggerRebuild):
                    AudioServicesRebuildSource source = queue.DequeueAudioServicesRebuildSource();
                    AudioServicesRebuildLevel level = queue.DequeueAudioServicesRebuildLevel();
                    audioServicesRepo.TriggerRebuild(source, level);
                    return Task.FromResult<byte[]>(null);

                default:
                    throw new NotSupportedException($"Received action is not supported: {subTopic}");
            }
        }

        protected override void SubscribeToService()
        {
            audioServicesRepo.TriggeredRebuild += OnTriggeredRebuild;
        }

        protected override void UnsubscribeFromService()
        {
            audioServicesRepo.TriggeredRebuild -= OnTriggeredRebuild;
        }

        private async void OnTriggeredRebuild(object sender, AudioServicesTriggeredRebuildArgs e)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(e.Source)
                .Enqueue(e.Level);

            await SendAsync(nameof(audioServicesRepo.TriggeredRebuild), payload);
        }
    }
}
