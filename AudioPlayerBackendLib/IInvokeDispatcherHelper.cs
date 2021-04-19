using System;
using System.Threading.Tasks;

namespace AudioPlayerBackend
{
    public interface IInvokeDispatcherHelper
    {
        Task InvokeDispatcher(Action action);

        Task<T> InvokeDispatcher<T>(Func<T> func);
    }
}
