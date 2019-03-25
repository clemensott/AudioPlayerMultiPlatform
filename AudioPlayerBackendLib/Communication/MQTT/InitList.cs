using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StdOttStandard;

namespace AudioPlayerBackend.Communication.MQTT
{
    class InitList<T>
    {
        private readonly List<T> initItems;

        public Task Task { get; }

        public InitList(IEnumerable<T> items)
        {
            initItems = items.ToList();
            Task = Utils.WaitAsync(initItems);
        }

        public void Add(T item)
        {
            initItems.Add(item);
        }

        public void AddRange(IEnumerable<T> items)
        {
            initItems.AddRange(items);
        }

        public void Remove(T item)
        {
            lock (initItems)
            {
                initItems.Remove(item);

                if (initItems.Count == 0) Monitor.Pulse(initItems);
            }
        }
    }
}
