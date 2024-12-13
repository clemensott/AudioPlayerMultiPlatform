using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Build.Repo
{
    public interface IAudioServicesRepo
    {
        Task TriggerRebuild(AudioServicesRebuildSource source, AudioServicesRebuildLevel level);
        event EventHandler<AudioServicesTriggeredRebuildArgs> TriggeredRebuild;
    }
}
