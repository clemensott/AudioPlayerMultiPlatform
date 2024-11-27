using System;

namespace AudioPlayerBackend.Build.Repo
{
    internal class RootAudioServicesRepo : IAudioServicesRepo
    {
        public event EventHandler<AudioServicesTriggeredRebuildArgs> TriggeredRebuild;

        public void TriggerRebuild(AudioServicesRebuildSource source, AudioServicesRebuildLevel level)
        {
            TriggeredRebuild?.Invoke(this, new AudioServicesTriggeredRebuildArgs(source, level));
        }
    }
}
