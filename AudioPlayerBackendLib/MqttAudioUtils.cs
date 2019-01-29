using AudioPlayerBackend.Common;
using StdOttStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AudioPlayerBackend
{
    static class MqttAudioUtils
    {
        public static void LockTopic(Dictionary<string, byte[]> dict, string topic, byte[] payload)
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

        public static bool IsTopicLocked(Dictionary<string, byte[]> dict, string topic, byte[] payload)
        {
            byte[] payloadLock;

            if (!dict.TryGetValue(topic, out payloadLock)) return false;

            return payload.BothNullOrSequenceEqual(payloadLock);
        }

        public static bool UnlockTopic(Dictionary<string, byte[]> dict, string topic, bool pulseAll = false)
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

        public static bool UnlockTopic(Dictionary<string, byte[]> dict, string topic, byte[] payload, bool pulseAll = false)
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

        public static bool ContainsPlaylist(this IMqttAudio audio, string rawTopic, out string topic, out IPlaylist playlist)
        {
            if (!rawTopic.Contains('.'))
            {
                topic = rawTopic;
                playlist = null;
                return false;
            }

            string playlistID = rawTopic.Remove(36);
            playlist = audio.GetPlaylist(Guid.Parse(playlistID));
            topic = rawTopic.Substring(37);

            return true;
        }

        public static bool TryHandleMessage(this IMqttAudio audio, string topic, ByteQueue queue)
        {
            switch (topic)
            {
                case nameof(audio.AdditionalPlaylists):
                    IPlaylist[] playlists = queue.DequeuePlaylists();

                    for (int i = 0; i < playlists.Length; i++)
                    {
                        IPlaylist playlist;
                        if (audio.AdditionalPlaylists.TryFirst(out playlist, p => p.ID == playlists[i].ID)) playlists[i] = playlist;
                    }

                    foreach (IPlaylistExtended playlist in audio.AdditionalPlaylists.Except(playlists))
                    {
                        audio.AdditionalPlaylists.Remove(playlist);
                    }

                    for (int i = 0; i < playlists.Length; i++)
                    {
                        if (playlists[i].ID != audio.AdditionalPlaylists[i].ID)
                        {
                            audio.AdditionalPlaylists.Insert(i, (IPlaylistExtended)playlists[i]);
                        }
                    }
                    break;

                case nameof(audio.AudioData):
                    audio.AudioData = queue;
                    break;

                case nameof(audio.CurrentPlaylist):
                    audio.CurrentPlaylist = queue.Any() ? audio.GetPlaylist(new Guid(queue)) : null;
                    break;

                case nameof(audio.FileMediaSources):
                    audio.FileMediaSources = queue.DequeueStrings();
                    break;

                case nameof(audio.Format):
                    audio.Format = queue.DequeueWaveFormat();
                    break;

                case nameof(audio.PlayState):
                    audio.PlayState = (PlaybackState)queue.DequeueInt();
                    break;

                case nameof(audio.Volume):
                    audio.Volume = queue.DequeueFloat();
                    break;

                default:
                    return false;
            }

            return true;
        }

        public static bool TryHandleMessage(this IMqttAudio audio, string topic, ByteQueue queue, IPlaylist playlist)
        {
            switch (topic)
            {
                case nameof(playlist.CurrentSong):
                    playlist.CurrentSong = queue.Any() ? (Song?)queue.DequeueSong() : null;
                    break;

                case nameof(playlist.Duration):
                    playlist.Duration = queue.DequeueTimeSpan();
                    break;

                case nameof(playlist.IsAllShuffle):
                    playlist.IsAllShuffle = queue.DequeueBool();
                    break;

                case nameof(playlist.IsOnlySearch):
                    playlist.IsOnlySearch = queue.DequeueBool();
                    break;

                case nameof(playlist.IsSearchShuffle):
                    playlist.IsSearchShuffle = queue.DequeueBool();
                    break;

                case nameof(playlist.Loop):
                    playlist.Loop = (LoopType)queue.DequeueInt();
                    break;

                case nameof(playlist.Position):
                    playlist.Position = queue.DequeueTimeSpan();
                    break;

                case nameof(playlist.SearchKey):
                    playlist.SearchKey = queue.DequeueString();
                    break;

                case nameof(playlist.Songs):
                    playlist.Songs = queue.DequeueSongs();
                    break;

                default:
                    return false;
            }

            return true;
        }

    }
}
