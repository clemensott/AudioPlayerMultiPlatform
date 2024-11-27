namespace AudioPlayerBackend.Build.Repo
{
    public class AudioServicesTriggeredRebuildArgs
    {
        public AudioServicesRebuildSource Source { get; private set; }

        public AudioServicesRebuildLevel Level { get; private set; }

        public AudioServicesTriggeredRebuildArgs(AudioServicesRebuildSource source, AudioServicesRebuildLevel level)
        {
            Source = source;
            Level = level;
        }
    }
}
