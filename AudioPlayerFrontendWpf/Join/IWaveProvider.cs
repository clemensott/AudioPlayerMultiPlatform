using AudioPlayerBackend.Common;

namespace AudioPlayerFrontend.Join
{
    interface IWaveProvider : NAudio.Wave.IWaveProvider, AudioPlayerBackend.Common.IWaveProvider
    {
    }
}
