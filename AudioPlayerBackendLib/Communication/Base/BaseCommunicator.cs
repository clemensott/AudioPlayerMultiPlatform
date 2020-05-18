﻿using System;
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
        protected readonly INotifyPropertyChangedHelper helper;
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

        protected BaseCommunicator(INotifyPropertyChangedHelper helper)
        {
            this.helper = helper;
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
            service.AudioFormatChanged += OnServiceAudioFormatChanged;
            service.CurrentPlaylistChanged += OnServiceCurrentPlaylistChanged;
            service.PlaylistsChanged += OnServicePlaylistsChanged;
            service.PlayStateChanged += OnServicePlayStateChanged;
            service.VolumeChanged += OnServiceVolumeChanged;

            Subscribe(service.SourcePlaylist);
            Subscribe(service.Playlists);
        }

        protected void Unsubscribe(IAudioServiceBase service)
        {
            if (service == null) return;

            service.AudioDataChanged -= OnServiceAudioDataChanged;
            service.AudioFormatChanged -= OnServiceAudioFormatChanged;
            service.CurrentPlaylistChanged -= OnServiceCurrentPlaylistChanged;
            service.PlaylistsChanged -= OnServicePlaylistsChanged;
            service.PlayStateChanged -= OnServicePlayStateChanged;
            service.VolumeChanged -= OnServiceVolumeChanged;

            Unsubscribe(service.SourcePlaylist);
            Unsubscribe(service.Playlists);
        }

        private void Subscribe(ISourcePlaylistBase playlist)
        {
            if (playlist == null) return;

            Subscribe((IPlaylistBase)playlist);

            playlist.FileMediaSourcesChanged += OnPlaylistFileMediaSourcesChanged;
            playlist.IsSearchShuffleChanged += OnPlaylistIsSearchShuffleChanged;
            playlist.SearchKeyChanged += OnPlaylistSearchKeyChanged;
        }

        private void Unsubscribe(ISourcePlaylistBase playlist)
        {
            if (playlist == null) return;

            Unsubscribe((IPlaylistBase)playlist);

            playlist.FileMediaSourcesChanged -= OnPlaylistFileMediaSourcesChanged;
            playlist.IsSearchShuffleChanged -= OnPlaylistIsSearchShuffleChanged;
            playlist.SearchKeyChanged -= OnPlaylistSearchKeyChanged;
        }

        private void Subscribe(IEnumerable<IPlaylistBase> playlists)
        {
            foreach (IPlaylistBase playlist in playlists.ToNotNull()) Subscribe(playlist);
        }

        private void Unsubscribe(IEnumerable<IPlaylistBase> playlists)
        {
            foreach (IPlaylistBase playlist in playlists.ToNotNull()) Unsubscribe(playlist);
        }

        protected void Subscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged += OnPlaylistCurrentSongChanged;
            playlist.DurationChanged += OnPlaylistDurationChanged;
            playlist.IsAllShuffleChanged += OnPlaylistIsAllShuffleChanged;
            playlist.LoopChanged += OnPlaylistLoopChanged;
            playlist.PositionChanged += OnPlaylistPositionChanged;
            playlist.WannaSongChanged += OnPlaylistWannaSongChanged;
            playlist.SongsChanged += OnPlaylistSongsChanged;
        }

        protected void Unsubscribe(IPlaylistBase playlist)
        {
            if (playlist == null) return;

            playlist.CurrentSongChanged -= OnPlaylistCurrentSongChanged;
            playlist.DurationChanged -= OnPlaylistDurationChanged;
            playlist.IsAllShuffleChanged -= OnPlaylistIsAllShuffleChanged;
            playlist.LoopChanged -= OnPlaylistLoopChanged;
            playlist.PositionChanged -= OnPlaylistPositionChanged;
            playlist.WannaSongChanged -= OnPlaylistWannaSongChanged;
            playlist.SongsChanged -= OnPlaylistSongsChanged;
        }


        protected virtual void OnServiceAudioDataChanged(object sender, ValueChangedEventArgs<byte[]> e)
        {
        }

        protected virtual void OnServiceAudioFormatChanged(object sender, ValueChangedEventArgs<WaveFormat> e)
        {
        }

        protected virtual void OnServiceCurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
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

        protected virtual void OnPlaylistFileMediaSourcesChanged(object sender, ValueChangedEventArgs<string[]> e)
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

        protected virtual void OnPlaylistIsAllShuffleChanged(object sender, ValueChangedEventArgs<bool> e)
        {
        }

        protected virtual void OnPlaylistLoopChanged(object sender, ValueChangedEventArgs<LoopType> e)
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