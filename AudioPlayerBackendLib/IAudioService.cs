using System.Threading.Tasks;

namespace AudioPlayerBackend
{
    public interface IAudioService
    {
        Task Start();

        Task Stop();

        Task Dispose();
    }
}
