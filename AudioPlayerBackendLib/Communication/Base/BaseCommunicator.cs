using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using AudioPlayerBackend.Player;
using StdOttStandard.Linq;

namespace AudioPlayerBackend.Communication.Base
{
    public abstract class BaseCommunicator : ICommunicator, INotifyPropertyChanged
    {
        protected const string cmdString = "Command";

        private bool isSyncing;
        private readonly Dictionary<string, byte[]> receivingDict = new Dictionary<string, byte[]>();
        protected readonly Dictionary<Guid, IPlaylistBase> playlists = new Dictionary<Guid, IPlaylistBase>();

        public abstract event EventHandler<DisconnectedEventArgs> Disconnected;

        public abstract bool IsOpen { get; }

        public abstract string Name { get; }

        public bool IsSyncing
        {
            get => isSyncing;
            protected set
            {
                if (value == isSyncing) return;

                isSyncing = value;
                OnPropertyChanged(nameof(IsSyncing));
            }
        }

        public IAudioServiceBase Service { get; protected set; }

        protected BaseCommunicator()
        {
        }

        public abstract Task OpenAsync(BuildStatusToken statusToken);

        public abstract Task SendCommand(string cmd);

        public abstract Task SetService(IAudioServiceBase service, BuildStatusToken statusToken);

        public abstract Task SyncService(BuildStatusToken statusToken);

        public abstract Task CloseAsync();

        public abstract void Dispose();

        protected void Subscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.AudioDataChanged += OnServiceAudioDataChanged;
            service.FileMediaSourceRootsChanged += OnFileMediaSourceRootsChanged;
            service.CurrentPlaylistChanged += OnServiceCurrentPlaylistChanged;
            service.SourcePlaylistsChanged += OnServiceSourcePlaylistsChanged;
            service.PlaylistsChanged += OnServicePlaylistsChanged;
            service.PlayStateChanged += OnServicePlayStateChanged;
            service.VolumeChanged += OnServiceVolumeChanged;
            service.IsSearchShuffleChanged += OnPlaylistIsSearchShuffleChanged;
            service.SearchKeyChanged += OnPlaylistSearchKeyChanged;

            service.SourcePlaylists.ForEach(Subscribe);
            service.Playlists.ForEach(Subscribe);
        }

        protected void Unsubscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.AudioDataChanged -= OnServiceAudioDataChanged;
            service.FileMediaSourceRootsChanged -= OnFileMediaSourceRootsChanged;
            service.CurrentPlaylistChanged -= OnServiceCurrentPlaylistChanged;
            service.SourcePlaylistsChanged -= OnServiceSourcePlaylistsChanged;
            service.PlaylistsChanged -= OnServicePlaylistsChanged;
            service.PlayStateChanged -= OnServicePlayStateChanged;
            service.VolumeChanged -= OnServiceVolumeChanged;
            service.IsSearchShuffleChanged -= OnPlaylistIsSearchShuffleChanged;
            service.SearchKeyChanged -= OnPlaylistSearchKeyChanged;

            service.SourcePlaylists.ForEach(Unsubscribe);
            service.Playlists.ForEach(Unsubscribe);
        }

        protected void Subscribe(ISourcePlaylistBase playlist)
        {
            if (playlist == null) return;

            Subscribe((IPlaylistBase)playlist);

            playlist.FileMediaSourcesChanged += OnPlaylistFileMediaSourcesChanged;
        }

        protected void Unsubscribe(ISourcePlaylistBase playlist)
        {
            if (playlist == null) return;

            Unsubscribe((IPlaylistBase)playlist);

            playlist.FileMediaSourcesChanged -= OnPlaylistFileMediaSourcesChanged;
        }

        protected void Subscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnPlaylistCurrentSongChanged;
            playlist.DurationChanged += OnPlaylistDurationChanged;
            playlist.ShuffleChanged += OnPlaylistShuffleChanged;
            playlist.LoopChanged += OnPlaylistLoopChanged;
            playlist.NameChanged += OnPlaylistNameChanged;
            playlist.PositionChanged += OnPlaylistPositionChanged;
            playlist.WannaSongChanged += OnPlaylistWannaSongChanged;
            playlist.SongsChanged += OnPlaylistSongsChanged;
        }

        protected void Unsubscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnPlaylistCurrentSongChanged;
            playlist.DurationChanged -= OnPlaylistDurationChanged;
            playlist.ShuffleChanged -= OnPlaylistShuffleChanged;
            playlist.LoopChanged -= OnPlaylistLoopChanged;
            playlist.NameChanged -= OnPlaylistNameChanged;
            playlist.PositionChanged -= OnPlaylistPositionChanged;
            playlist.WannaSongChanged -= OnPlaylistWannaSongChanged;
            playlist.SongsChanged -= OnPlaylistSongsChanged;
        }


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
