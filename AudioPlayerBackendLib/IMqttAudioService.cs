namespace AudioPlayerBackendLib
{
    public interface IMqttAudioService : IMqttAudio
    {
        int Port { get; }
    }
}
