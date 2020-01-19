using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.MQTT
{
    public class InitList<T> : ObservableCollection<T>
    {
        private readonly SemaphoreSlim finishedSem;

        public Task Task { get; }

        public InitList(IEnumerable<T> items) : base(items)
        {
            finishedSem = new SemaphoreSlim(0);
            Task = finishedSem.WaitAsync();
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                Add(item);
            }
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            finishedSem.Release();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            if (Count == 0) finishedSem.Release();
        }
    }
}
