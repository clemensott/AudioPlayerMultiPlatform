using MQTTnet;
using System.Collections.Generic;
using System.Threading;

namespace AudioPlayerBackend.Communication.MQTT
{
    class PublishQueue
    {
        private readonly Queue<string> queue;
        private readonly Dictionary<string, MqttApplicationMessage> dict;

        public PublishQueue()
        {
            queue = new Queue<string>();
            dict = new Dictionary<string, MqttApplicationMessage>();
        }

        public void Enqueue(MqttApplicationMessage message)
        {
            lock (queue)
            {
                MqttApplicationMessage enqueuedMessage;

                if (dict.TryGetValue(message.Topic, out enqueuedMessage))
                {
                    enqueuedMessage.Payload = message.Payload;
                    enqueuedMessage.QualityOfServiceLevel = message.QualityOfServiceLevel;
                    enqueuedMessage.Retain = message.Retain;
                }
                else
                {
                    queue.Enqueue(message.Topic);
                    dict.Add(message.Topic, message);

                    Monitor.Pulse(queue);
                }
            }
        }

        public MqttApplicationMessage Dequeue()
        {
            lock (queue)
            {
                while (queue.Count == 0) Monitor.Wait(queue);

                string topic = queue.Dequeue();
                MqttApplicationMessage message = dict[topic];

                dict.Remove(topic);

                return message;
            }
        }

        public bool IsEnqueued(string topic)
        {
            return dict.ContainsKey(topic);
        }
    }
}
