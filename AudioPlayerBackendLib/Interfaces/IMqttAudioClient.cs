namespace AudioPlayerBackend
{
    public interface IMqttAudioClient : IMqttAudio
    {
        int? Port { get; }

        string ServerAddress { get; }

        bool IsStreaming { get; set; }

        float ClientVolume { get; set; }
    }
}
