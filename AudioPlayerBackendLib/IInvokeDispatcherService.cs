using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend
{
    public interface IInvokeDispatcherService
    {
        Task InvokeDispatcher(Action action);

        Task<T> InvokeDispatcher<T>(Func<T> func);
    }
}
