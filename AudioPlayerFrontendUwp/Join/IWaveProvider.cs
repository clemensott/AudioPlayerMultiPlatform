namespace AudioPlayerFrontend.Join
{
    interface IWaveProvider : NAudio.Wave.IWaveProvider, AudioPlayerBackend.Player.IWaveProvider
    {
    }
}
