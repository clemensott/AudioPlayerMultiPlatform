using AudioPlayerBackend.Audio;

namespace AudioPlayerBackend.Player
{
    public class AudioStreamPlayer : IServicePlayer
    {
        private readonly IAudioStreamHelper helper;
        private IBufferedWaveProvider buffer;

        public IAudioService Service { get; }

        public IWaveProviderPlayer Player { get; }

        public AudioStreamPlayer(IAudioService service, IWaveProviderPlayer player, IAudioStreamHelper helper = null)
        {
            Service = service;
            Player = player;
            this.helper = helper;

            Subscribe(service);
        }

        private void Subscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.AudioFormatChanged += Service_AudioFormatChanged;
            service.AudioDataChanged += Service_AudioDataChanged;
        }

        private void Service_AudioFormatChanged(object sender, ValueChangedEventArgs<WaveFormat> e)
        {
            buffer = GetBufferedWaveProvider(e.NewValue);
            Player.Play(() => buffer);
        }

        private void Service_AudioDataChanged(object sender, ValueChangedEventArgs<byte[]> e)
        {
            if (buffer != null)
            {
                buffer.AddSamples(e.NewValue, 0, e.NewValue.Length);

                Player.PlayState = Service.PlayState;
            }
        }

        private IBufferedWaveProvider GetBufferedWaveProvider(WaveFormat format)
        {
            if (buffer == null) buffer = CreateBufferedWaveProvider(Service.AudioFormat);
            else if (buffer.WaveFormat != Service.AudioFormat)
            {
                buffer.ClearBuffer();
                buffer = CreateBufferedWaveProvider(Service.AudioFormat);
            }

            return buffer;
        }

        protected virtual IBufferedWaveProvider CreateBufferedWaveProvider(WaveFormat format)
        {
            return helper.CreateBufferedWaveProvider(format, Service);
        }

        public void Dispose()
        {
            Player?.Dispose();
            buffer?.Dispose();
        }
    }
}
