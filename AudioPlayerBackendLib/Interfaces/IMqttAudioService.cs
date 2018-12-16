namespace AudioPlayerBackend
{
    public interface IMqttAudioService : IMqttAudio,IAudioService
    {
        int Port { get; }
    }
}
