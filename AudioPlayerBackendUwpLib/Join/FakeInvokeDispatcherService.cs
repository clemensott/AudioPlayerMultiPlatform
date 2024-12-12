using AudioPlayerBackend;
using System;
using System.Threading.Tasks;

namespace AudioPlayerBackendUwpLib.Join
{
    public class FakeInvokeDispatcherService : IInvokeDispatcherService
    {
        public Task InvokeDispatcher(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public Task<T> InvokeDispatcher<T>(Func<T> func)
        {
            return Task.FromResult<T>(func());
        }
    }
}
