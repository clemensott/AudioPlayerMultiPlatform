using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using StdOttStandard.Linq;

namespace AudioPlayerBackend.Communication.Base
{
    public abstract class BaseCommunicator : ICommunicator, INotifyPropertyChanged
    {
        protected const string cmdString = "Command";

        private readonly Dictionary<string, byte[]> receivingDict = new Dictionary<string, byte[]>();

        public abstract event EventHandler<DisconnectedEventArgs> Disconnected;
        public abstract event EventHandler<ReceivedEventArgs> Received;

        public abstract bool IsOpen { get; }

        public abstract string Name { get; }

        protected BaseCommunicator()
        {
        }

        public abstract Task<bool> SendCommand(string cmd);

        public abstract Task<byte[]> SendAsync(string topic, byte[] payload);

        public abstract Task Start();

        public abstract Task Stop();

        public abstract Task Dispose();

        protected void LockTopic(string topic, byte[] payload)
        {
            LockTopic(receivingDict, topic, payload);
        }

        private static void LockTopic(Dictionary<string, byte[]> dict, string topic, byte[] payload)
        {
            // payload == null means that topic doesn't has to be lock, for example because it is a call to fetch data
            if (payload == null) return;

            byte[] payloadLock;

            while (true)
            {
                lock (dict)
                {
                    if (!dict.TryGetValue(topic, out payloadLock))
                    {
                        dict.Add(topic, payload);
                        return;
                    }
                }

                lock (payloadLock) Monitor.Wait(payloadLock);
            }
        }

        protected bool IsTopicLocked(string topic, byte[] payload)
        {
            return IsTopicLocked(receivingDict, topic, payload);
        }

        private static bool IsTopicLocked(Dictionary<string, byte[]> dict, string topic, byte[] payload)
        {
            if (payload == null) return false;

            byte[] payloadLock;

            if (!dict.TryGetValue(topic, out payloadLock)) return false;

            return payload.BothNullOrSequenceEqual(payloadLock);
        }

        protected bool UnlockTopic(string topic, bool pulseAll = false)
        {
            return UnlockTopic(receivingDict, topic, pulseAll);
        }

        private static bool UnlockTopic(Dictionary<string, byte[]> dict, string topic, bool pulseAll = false)
        {
            byte[] payloadLock;

            lock (dict)
            {
                if (!dict.TryGetValue(topic, out payloadLock)) return false;

                dict.Remove(topic);
            }

            lock (payloadLock)
            {
                if (pulseAll) Monitor.PulseAll(payloadLock);
                else Monitor.Pulse(payloadLock);
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
