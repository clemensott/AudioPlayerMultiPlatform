namespace AudioPlayerBackend.Player
{
    public interface IPlayerService : IAudioService
    {
        IPlayer Player { get; }
    }
}
