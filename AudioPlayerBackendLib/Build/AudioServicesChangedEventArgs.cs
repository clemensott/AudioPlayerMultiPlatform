namespace AudioPlayerBackend.Build
{
    public class AudioServicesChangedEventArgs : System.EventArgs
    {
        public AudioServices OldServices { get; }
        
        public AudioServices NewServices { get; }

        public AudioServicesChangedEventArgs(AudioServices oldServices, AudioServices newServices)
        {
            OldServices = oldServices;
            NewServices = newServices;
        }
    }
}
