namespace AudioPlayerBackend
{
    public interface IMqttAudioService : IMqttAudio
    {
        int Port { get; }
    }
}
