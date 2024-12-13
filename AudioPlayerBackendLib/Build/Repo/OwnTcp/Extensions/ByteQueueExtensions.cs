using AudioPlayerBackend.Communication.Base;

namespace AudioPlayerBackend.Build.Repo.OwnTcp.Extensions
{
    public static class ByteQueueExtensions
    {
        public static ByteQueue Enqueue(this ByteQueue queue, AudioServicesRebuildSource source)
        {
            return queue.Enqueue((int)source);
        }

        public static AudioServicesRebuildSource DequeueAudioServicesRebuildSource(this ByteQueue queue)
        {
            return (AudioServicesRebuildSource)queue.DequeueInt();
        }

        public static ByteQueue Enqueue(this ByteQueue queue, AudioServicesRebuildLevel level)
        {
            return queue.Enqueue((int)level);
        }

        public static AudioServicesRebuildLevel DequeueAudioServicesRebuildLevel(this ByteQueue queue)
        {
            return (AudioServicesRebuildLevel)queue.DequeueInt();
        }
    }
}
