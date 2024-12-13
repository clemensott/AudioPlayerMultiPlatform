using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Build.Repo
{
    public class RootAudioServicesRepo : IAudioServicesRepo
    {
        public event EventHandler<AudioServicesTriggeredRebuildArgs> TriggeredRebuild;

        public Task TriggerRebuild(AudioServicesRebuildSource source, AudioServicesRebuildLevel level)
        {
            TriggeredRebuild?.Invoke(this, new AudioServicesTriggeredRebuildArgs(source, level));
            return Task.CompletedTask;
        }
    }
}
