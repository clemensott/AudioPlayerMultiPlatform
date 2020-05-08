using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    class OwnTcpSendQueue
    {
        private readonly Queue<string> queue;
        private readonly Dictionary<string, OwnTcpSendMessage> dict;

        public bool IsEnded { get; private set; }

        public OwnTcpSendQueue()
        {
            queue = new Queue<string>();
            dict = new Dictionary<string, OwnTcpSendMessage>();
        }

        public Task Enqueue(OwnTcpMessage message)
        {
            lock (queue)
            {
                OwnTcpSendMessage sendMessage;
                if (dict.TryGetValue(message.Topic, out sendMessage)) sendMessage.Message = message;
                else
                {
                    sendMessage = new OwnTcpSendMessage(message);
                    queue.Enqueue(message.Topic);
                    dict.Add(message.Topic, sendMessage);

                    Monitor.Pulse(queue);
                }

                return sendMessage.Task;
            }
        }

        public OwnTcpSendMessage Dequeue()
        {
            lock (queue)
            {
                while (true)
                {
                    if (queue.Count > 0) break;
                    if (IsEnded) return null;

                    Monitor.Wait(queue);
                }

                string topic = queue.Dequeue();
                OwnTcpSendMessage message = dict[topic];

                dict.Remove(topic);

                return message;
            }
        }

        public void End()
        {
            IsEnded = true;

            lock (queue)
            {
                Monitor.Pulse(queue);
            }
        }

        public bool IsEnqueued(string topic)
        {
            return dict.ContainsKey(topic);
        }
    }
}
