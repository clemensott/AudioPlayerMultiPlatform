using System;
using System.Threading.Tasks;
using AudioPlayerBackend.Build.Repo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.OwnTcp;

namespace AudioPlayerBackend.Build.Repo.OwnTcp
{
    public class OwnTcpAudioServicesRepo : OwnTcpBaseService, IAudioServicesRepo
    {
        public event EventHandler<AudioServicesTriggeredRebuildArgs> TriggeredRebuild;

        public OwnTcpAudioServicesRepo(IClientCommunicator clientCommunicator)
            : base(nameof(IAudioServicesRepo), clientCommunicator)
        {
        }

        protected override Task<byte[]> OnMessageReceived(string subTopic, byte[] payload)
        {
            ByteQueue queue = payload;
            switch (subTopic)
            {
                case nameof(TriggeredRebuild):
                    AudioServicesRebuildSource source = queue.DequeueAudioServicesRebuildSource();
                    AudioServicesRebuildLevel level = queue.DequeueAudioServicesRebuildLevel();
                    TriggeredRebuild?.Invoke(this, new AudioServicesTriggeredRebuildArgs(source, level));
                    return Task.FromResult<byte[]>(null);

                default:
                    throw new NotSupportedException($"Received action is not supported: {subTopic}");
            }
        }

        public async Task TriggerRebuild(AudioServicesRebuildSource source, AudioServicesRebuildLevel level)
        {
            ByteQueue payload = new ByteQueue()
                .Enqueue(source)
                .Enqueue(level);

            await SendAsync(nameof(TriggerRebuild), payload);

            TriggeredRebuild?.Invoke(this, new AudioServicesTriggeredRebuildArgs(source, level));
        }
    }
}
