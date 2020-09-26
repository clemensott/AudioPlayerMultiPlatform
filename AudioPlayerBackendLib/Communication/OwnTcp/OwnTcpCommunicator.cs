using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.Player;

namespace AudioPlayerBackend.Communication.OwnTcp
{
    public abstract class OwnTcpCommunicator : BaseCommunicator
    {
        protected const string anwserCmd = "-ans", syncCmd = "-sync", pingCmd = "-ping", closeCmd = "-close";

        protected OwnTcpCommunicator(INotifyPropertyChangedHelper helper = null) : base(helper)
        {
        }

        //protected override void OnServiceAudioDataChanged(object sender, ValueChangedEventArgs<byte[]> e)
        //{
        //    await PublishAudioData();
        //}

        protected async Task PublishAudioData()
        {
            await SendAsync(nameof(Service.AudioData), Service.AudioData, true);
        }

        //protected override void OnServiceAudioFormatChanged(object sender, ValueChangedEventArgs<WaveFormat> e)
        //{
        //    await PublishFormat();
        //}

        protected async Task PublishFormat()
        {
            ByteQueue data = new ByteQueue();
            if (Service.AudioFormat != null) data.Enqueue(Service.AudioFormat);

            await SendAsync(nameof(Service.AudioFormat), data, false);
        }

        protected override async void OnServiceCurrentPlaylistChanged(object sender, ValueChangedEventArgs<IPlaylistBase> e)
        {
            await PublishCurrentPlaylist();
        }

        protected async Task PublishCurrentPlaylist()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(Service.CurrentPlaylist);

            await SendAsync(nameof(Service.CurrentPlaylist), data, false);
        }

        protected override async void OnServicePlaylistsChanged(object sender, ValueChangedEventArgs<IPlaylistBase[]> e)
        {
            List<IPlaylistBase> added = new List<IPlaylistBase>();

            foreach (IPlaylistBase playlist in e.OldValue.Except(e.NewValue))
            {
                Unsubscribe(playlist);
                if (playlists.ContainsKey(playlist.ID)) playlists.Remove(playlist.ID);
            }

            foreach (IPlaylistBase playlist in e.NewValue.Except(e.OldValue))
            {
                Subscribe(playlist);

                added.Add(playlist);
                playlists[playlist.ID] = playlist;
            }

            foreach (IPlaylistBase playlist in e.NewValue)
            {
                playlists[playlist.ID] = playlist;
            }

            ByteQueue queue = new ByteQueue();
            queue.Enqueue(added);
            queue.Enqueue(e.NewValue.Select(p => p.ID));

            await SendAsync(nameof(Service.Playlists), queue, false);
        }

        protected override async void OnServicePlayStateChanged(object sender, ValueChangedEventArgs<PlaybackState> e)
        {
            await PublishPlayState();
        }

        protected async Task PublishPlayState()
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)Service.PlayState);

            await SendAsync(nameof(Service.PlayState), data, false);
        }

        protected override async void OnServiceVolumeChanged(object sender, ValueChangedEventArgs<float> e)
        {
            await PublishVolume();
        }

        protected async Task PublishVolume()
        {
            try
            {
                ByteQueue data = new ByteQueue();
                data.Enqueue(Service.Volume);

                await SendAsync(nameof(Service.Volume), data, true);
            }
            catch { }
        }

        protected override async void OnPlaylistFileMediaSourcesChanged(object sender, ValueChangedEventArgs<string[]> e)
        {
            await PublishMediaSources((ISourcePlaylistBase)sender);
        }

        protected async Task PublishMediaSources(ISourcePlaylistBase source)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(source.FileMediaSources);

            await SendAsync(source, nameof(source.FileMediaSources), data, false);
        }

        protected override async void OnPlaylistCurrentSongChanged(object sender, ValueChangedEventArgs<Song?> e)
        {
            await PublishCurrentSong((IPlaylistBase)sender);
        }

        protected async Task PublishCurrentSong(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.CurrentSong);

            await SendAsync(playlist, nameof(playlist.CurrentSong), data, false);
        }

        protected override async void OnPlaylistDurationChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            await PublishDuration((IPlaylistBase)sender);
        }

        protected async Task PublishDuration(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Duration);

            await SendAsync(playlist, nameof(playlist.Duration), data, false);
        }

        protected override async void OnPlaylistIsAllShuffleChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            await PublishIsAllShuffle((IPlaylistBase)sender);
        }

        protected async Task PublishIsAllShuffle(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.IsAllShuffle);

            await SendAsync(playlist, nameof(playlist.IsAllShuffle), data, false);
        }

        protected override async void OnPlaylistLoopChanged(object sender, ValueChangedEventArgs<LoopType> e)
        {
            await PublishLoop((IPlaylistBase)sender);
        }

        protected async Task PublishLoop(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue((int)playlist.Loop);

            await SendAsync(playlist, nameof(playlist.Loop), data, false);
        }

        protected override async void OnPlaylistPositionChanged(object sender, ValueChangedEventArgs<TimeSpan> e)
        {
            await PublishPosition((IPlaylistBase)sender);
        }

        protected async Task PublishPosition(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Position);

            await SendAsync(playlist, nameof(playlist.Position), data, false);
        }

        protected override async void OnPlaylistWannaSongChanged(object sender, ValueChangedEventArgs<RequestSong?> e)
        {
            await PublishWannaSong((IPlaylistBase)sender);
        }

        private async Task PublishWannaSong(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.WannaSong);

            await SendAsync(playlist, nameof(playlist.WannaSong), data, false);
        }

        protected override async void OnPlaylistSongsChanged(object sender, ValueChangedEventArgs<Song[]> e)
        {
            await PublishSongs((IPlaylistBase)sender);
        }

        protected async Task PublishSongs(IPlaylistBase playlist)
        {
            ByteQueue data = new ByteQueue();
            data.Enqueue(playlist.Songs);

            await SendAsync(playlist, nameof(playlist.Songs), data, false);
        }

        private Task SendAsync(IPlaylistBase playlist, string topic, byte[] payload, bool fireAndForget)
        {
            return SendAsync(playlist.ID + "." + topic, payload, fireAndForget);
        }

        protected abstract Task SendAsync(string topic, byte[] payload, bool fireAndForget);

        protected static IEnumerable<byte> GetBytes(OwnTcpMessage message)
        {
            byte[] idBytes = BitConverter.GetBytes(message.ID);
            byte[] fireAndForgetBytes = BitConverter.GetBytes(message.IsFireAndForget);
            byte[] topicBytes = Encoding.UTF8.GetBytes(message.Topic);
            byte[] topicLengthBytes = BitConverter.GetBytes(topicBytes.Length);
            byte[] payloadLengthBytes = BitConverter.GetBytes(message.Payload?.Length ?? -1);

            return idBytes.Concat(fireAndForgetBytes)
                .Concat(topicLengthBytes)
                .Concat(topicBytes)
                .Concat(payloadLengthBytes)
                .Concat(message.Payload ?? new byte[0]);
        }

        protected static async Task<OwnTcpMessage> ReadMessage(Stream stream)
        {
            byte[] idBytes = await ReadAsync(stream, sizeof(uint));
            if (idBytes == null) return null;

            uint id = BitConverter.ToUInt32(idBytes, 0);
            bool fireAndForget = BitConverter.ToBoolean(await ReadAsync(stream, sizeof(bool)), 0);
            int topicLength = BitConverter.ToInt32(await ReadAsync(stream, sizeof(int)), 0);
            string topic = Encoding.UTF8.GetString(await ReadAsync(stream, topicLength));
            int payloadLength = BitConverter.ToInt32(await ReadAsync(stream, sizeof(int)), 0);

            byte[] payload;
            if (payloadLength > 0) payload = await ReadAsync(stream, payloadLength);
            else if (payloadLength == 0) payload = new byte[0];
            else payload = null;

            return new OwnTcpMessage()
            {
                IsFireAndForget = fireAndForget,
                ID = id,
                Topic = topic,
                Payload = payload,
            };
        }

        private static async Task<byte[]> ReadAsync(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int remainingCount = count;
            do
            {
                int readCount = await stream.ReadAsync(buffer, count - remainingCount, remainingCount);
                if (readCount == 0) return null;

                remainingCount -= readCount;
            } while (remainingCount > 0);

            return buffer;
        }

        protected bool HandlerMessage(OwnTcpMessage message)
        {
            string topic;
            Guid id;

            return ContainsPlaylist(message.Topic, out topic, out id) ?
                HandlePlaylistMessage(id, topic, message.Payload) :
                HandleServiceMessage(topic, message.Payload);
        }

        private static bool ContainsPlaylist(string rawTopic, out string topic, out Guid id)
        {
            if (!rawTopic.Contains('.'))
            {
                topic = rawTopic;
                id = Guid.Empty;
                return false;
            }

            id = Guid.Parse(rawTopic.Remove(36));
            topic = rawTopic.Substring(37);

            return true;
        }

        private bool HandleServiceMessage(string topic, ByteQueue data)
        {
            switch (topic)
            {
                case nameof(Service.Playlists):
                    HandlePlaylistsTopic(data);
                    break;

                case nameof(Service.AudioData):
                    Service.AudioData = data;
                    break;

                case nameof(Service.CurrentPlaylist):
                    HandleCurrentPlaylistTopic(data);
                    break;

                case nameof(Service.AudioFormat):
                    Service.AudioFormat = data.DequeueWaveFormat();
                    break;

                case nameof(Service.PlayState):
                    Service.PlayState = (PlaybackState)data.DequeueInt();
                    break;

                case nameof(Service.Volume):
                    Service.Volume = data.DequeueFloat();
                    break;

                case cmdString:
                    IAudioService service;
                    string cmd = Encoding.UTF8.GetString(data);

                    switch (cmd.ToLower())
                    {
                        case "play":
                            Service.PlayState = PlaybackState.Playing;
                            break;

                        case "pause":
                            Service.PlayState = PlaybackState.Paused;
                            break;

                        case "toggle":
                            Service.PlayState = Service.PlayState != PlaybackState.Playing
                                ? PlaybackState.Playing
                                : PlaybackState.Paused;
                            break;

                        case "next":
                            service = Service as IAudioService;
                            service?.SetNextSong();
                            break;

                        case "previous":
                            service = Service as IAudioService;
                            service?.SetPreviousSong();
                            break;

                        default:
                            return false;
                    }
                    break;

                default:
                    return false;
            }

            return true;
        }

        private void HandlePlaylistsTopic(ByteQueue data)
        {
            Dictionary<Guid, IPlaylistBase> addPlaylists = data
                    .DequeuePlaylists(id => new Playlist(id, helper))
                    .ToDictionary(p => p.ID);
            Guid[] order = data.DequeueGuids();
            IPlaylistBase[] newPlaylists = new IPlaylistBase[order.Length];

            for (int i = 0; i < order.Length; i++)
            {
                IPlaylistBase playlist;
                Guid id = order[i];
                if (addPlaylists.TryGetValue(id, out playlist))
                {
                    newPlaylists[i] = playlists[id] = playlist;
                }
                else newPlaylists[i] = playlists[id];
            }

            Service.Playlists = newPlaylists;
        }

        private void HandleCurrentPlaylistTopic(ByteQueue data)
        {
            IPlaylistBase existingPlaylist;
            IPlaylistBase currentPlaylist = data.DequeuePlaylist(id => new Playlist(id, helper));

            if (playlists.TryGetValue(currentPlaylist.ID, out existingPlaylist))
            {
                Service.CurrentPlaylist = existingPlaylist;
            }
            else
            {
                playlists.Add(currentPlaylist.ID, currentPlaylist);
                Service.CurrentPlaylist = currentPlaylist;
            }
        }

        private bool HandlePlaylistMessage(Guid id, string topic, ByteQueue data)
        {
            IPlaylistBase playlist = GetPlaylist(id);
            ISourcePlaylistBase source = playlist as ISourcePlaylistBase;

            switch (topic)
            {
                case nameof(playlist.CurrentSong):
                    playlist.CurrentSong = data.DequeueNullableSong();
                    break;

                case nameof(playlist.Duration):
                    playlist.Duration = data.DequeueTimeSpan();
                    break;

                case nameof(playlist.IsAllShuffle):
                    playlist.IsAllShuffle = data.DequeueBool();
                    break;

                case nameof(playlist.Loop):
                    playlist.Loop = (LoopType)data.DequeueInt();
                    break;

                case nameof(playlist.Position):
                    playlist.Position = data.DequeueTimeSpan();
                    break;

                case nameof(playlist.WannaSong):
                    playlist.WannaSong = data.DequeueNullableRequestSong();
                    break;

                case nameof(playlist.Songs):
                    playlist.Songs = data.DequeueSongs();
                    break;

                case nameof(source.IsSearchShuffle):
                    source.IsSearchShuffle = data.DequeueBool();
                    break;

                case nameof(source.SearchKey):
                    source.SearchKey = data.DequeueString();
                    break;

                case nameof(source.FileMediaSources):
                    source.FileMediaSources = data.DequeueStrings();
                    break;

                default:
                    return false;
            }

            return true;
        }

        private IPlaylistBase GetPlaylist(Guid id)
        {
            return id == Guid.Empty ? Service.SourcePlaylist : playlists[id];
        }
    }
}
