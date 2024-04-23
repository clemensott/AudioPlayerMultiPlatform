using AudioPlayerBackend.Audio;

namespace AudioPlayerBackend.Player
{
    public class AudioStreamPlayer : IServicePlayer
    {
        public IAudioService Service { get; }

        public IPlayer Player { get; }

        public AudioStreamPlayer(IAudioService service, IPlayer player)
        {
            Service = service;
            Player = player;

            Subscribe(service);
        }

        private void Subscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.AudioDataChanged += Service_AudioDataChanged;
        }

        private void Service_AudioDataChanged(object sender, ValueChangedEventArgs<byte[]> e)
        {
            /// TODO: Use data
        }

        public void Dispose()
        {
            Player?.Dispose();
        }
    }
}
