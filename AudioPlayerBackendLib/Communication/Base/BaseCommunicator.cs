using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Audio.MediaSource;
using AudioPlayerBackend.Player;
using StdOttStandard.Linq;

namespace AudioPlayerBackend.Communication.Base
{
    public abstract class BaseCommunicator : ICommunicator, INotifyPropertyChanged
    {
        protected const string cmdString = "Command";

        private readonly Dictionary<string, byte[]> receivingDict = new Dictionary<string, byte[]>();

        public abstract event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<ReceivedEventArgs> Received;

        public abstract bool IsOpen { get; }

        public abstract string Name { get; }

        protected BaseCommunicator()
        {
        }

        public abstract Task SendCommand(string cmd);

        public abstract Task<byte[]> SendAsync(string topic, byte[] payload);

        public abstract Task Start();

        public abstract Task Stop();

        public abstract Task Dispose();

        protected virtual void OnServiceAudioDataChanged(object sender, ValueChangedEventArgs<byte[]> e)
        {
        }

        protected virtual void OnFileMediaSourceRootsChanged(object sender, ValueChangedEventArgs<FileMediaSourceRoot[]> e)
        {
        }

        protected virtual void OnServiceCurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
        }

        protected virtual void OnServiceSourcePlaylistsChanged(object sender, ValueChangedEventArgs<ISourcePlaylistBase[]> e)
        {
        }

        protected virtual void OnServicePlaylistsChanged(object sender, ValueChangedEventArgs<IPlaylistBase[]> e)
        {
        }

        protected virtual void OnServicePlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
        }

        protected virtual void OnServiceVolumeChanged(object sender, ValueChangedEventArgs<float> e)
        {
        }

        protected virtual void OnPlaylistFileMediaSourcesChanged(object sender, ValueChangedEventArgs<FileMediaSource[]> e)
        {
        }

        protected virtual void OnPlaylistIsSearchShuffleChanged(object sender, ValueChangedEventArgs<bool> e)
        {
        }

        protected virtual void OnPlaylistSearchKeyChanged(object sender, ValueChangedEventArgs<string> e)
        {
        }

        protected virtual void OnPlaylistCurrentSongChanged(object sender, ValueChangedEventArgs<Song?> e)
        {
        }

        protected virtual void OnPlaylistDurationChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
        }

        protected virtual void OnPlaylistShuffleChanged(object sender, ValueChangedEventArgs<OrderType> e)
        {
        }

        protected virtual void OnPlaylistLoopChanged(object sender, ValueChangedEventArgs<LoopType> e)
        {
        }

        protected virtual void OnPlaylistNameChanged(object sender, ValueChangedEventArgs<string> e)
        {
        }

        protected virtual void OnPlaylistPositionChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
        }

        protected virtual void OnPlaylistWannaSongChanged(object sender, ValueChangedEventArgs<RequestSong?> e)
        {
        }

        protected virtual void OnPlaylistSongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
        }

        protected static string GetPlaylistType(IPlaylistBase playlist)
        {
            switch (playlist)
            {
                case ISourcePlaylistBase _:
                    return nameof(ISourcePlaylistBase);

                case IPlaylistBase _:
                    return nameof(IPlaylistBase);
            }

            return null;
        }

        protected void LockTopic(string topic, byte[] payload)
        {
            LockTopic(receivingDict, topic, payload);
        }

        private static void LockTopic(Dictionary<string, byte[]> dict, string topic, byte[] payload)
        {
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

        protected bool UnlockTopic(string topic, byte[] payload, bool pulseAll = false)
        {
            return UnlockTopic(receivingDict, topic, payload, pulseAll);
        }

        private static bool UnlockTopic(Dictionary<string, byte[]> dict, string topic, byte[] payload, bool pulseAll = false)
        {
            byte[] payloadLock;

            lock (dict)
            {
                if (!dict.TryGetValue(topic, out payloadLock) || payloadLock.BothNullOrSequenceEqual(payload)) return false;

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
