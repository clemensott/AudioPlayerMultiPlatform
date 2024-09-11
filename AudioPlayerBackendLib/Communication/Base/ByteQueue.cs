using StdOttStandard.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioPlayerBackend.Communication.Base
{
    class ByteQueue : IEnumerable<byte>
    {
        private readonly Queue<byte> bytes;

        public ByteQueue()
        {
            bytes = new Queue<byte>();
        }

        public ByteQueue(IEnumerable<byte> bytes)
        {
            this.bytes = new Queue<byte>(bytes);
        }

        public void EnqueueRange(IEnumerable<byte> enqueueBytes)
        {
            foreach (byte item in enqueueBytes) bytes.Enqueue(item);
        }

        public byte[] DequeueRange(int count)
        {
            byte[] dequeueBytes = new byte[count];

            for (int i = 0; i < count; i++) dequeueBytes[i] = bytes.Dequeue();

            return dequeueBytes;
        }

        public void Enqueue(bool value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
        }

        public bool DequeueBool()
        {
            return BitConverter.ToBoolean(DequeueRange(sizeof(bool)), 0);
        }

        public void Enqueue(ushort value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
        }

        public ushort DequeueUShort()
        {
            return BitConverter.ToUInt16(DequeueRange(sizeof(ushort)), 0);
        }

        public void Enqueue(int value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
        }

        public int DequeueInt()
        {
            return BitConverter.ToInt32(DequeueRange(sizeof(int)), 0);
        }

        public void Enqueue(long value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
        }

        public long DequeueLong()
        {
            return BitConverter.ToInt64(DequeueRange(sizeof(long)), 0);
        }

        public float DequeueFloat()
        {
            return BitConverter.ToSingle(DequeueRange(sizeof(float)), 0);
        }

        public void Enqueue(float value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
        }

        public float DequeueDouble()
        {
            return BitConverter.ToSingle(DequeueRange(sizeof(double)), 0);
        }

        public void Enqueue(double value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
        }

        public string DequeueString()
        {
            int length = DequeueInt();
            return length >= 0 ? Encoding.UTF8.GetString(DequeueRange(length)) : null;
        }

        public void Enqueue(string value)
        {
            if (value == null) Enqueue(-1);
            else
            {
                byte[] valueBytes = Encoding.UTF8.GetBytes(value);

                Enqueue(valueBytes.Length);
                EnqueueRange(valueBytes);
            }
        }

        public void Enqueue(IEnumerable<string> strings)
        {
            Enqueue(strings, Enqueue);
        }

        public string[] DequeueStrings()
        {
            return DequeueArray(DequeueString);
        }

        public void Enqueue(DateTime? value)
        {
            EnqueueNullable(value, Enqueue);
        }

        public DateTime? DequeueDateTimeNullable()
        {
            return DequeueNullable(DequeueDateTime);
        }

        public void Enqueue(DateTime value)
        {
            Enqueue(value.Ticks);
        }

        public DateTime DequeueDateTime()
        {
            return new DateTime(DequeueLong());
        }

        public void Enqueue(Song song)
        {
            Enqueue(song.Index);
            Enqueue(song.Artist);
            Enqueue(song.FullPath);
            Enqueue(song.Title);
        }

        public Song DequeueSong()
        {
            return new Song()
            {
                Index = DequeueInt(),
                Artist = DequeueString(),
                FullPath = DequeueString(),
                Title = DequeueString()
            };
        }

        public void Enqueue(Song? song)
        {
            EnqueueNullable(song, Enqueue);
        }

        public Song? DequeueNullableSong()
        {
            return DequeueNullable(DequeueSong);
        }

        public void Enqueue(IEnumerable<Song> songs)
        {
            Enqueue(songs, Enqueue);
        }

        public Song[] DequeueSongs()
        {
            return DequeueArray(DequeueSong);
        }

        public void Enqueue(TimeSpan span)
        {
            Enqueue(span.Ticks);
        }

        public TimeSpan DequeueTimeSpan()
        {
            return TimeSpan.FromTicks(DequeueLong());
        }

        public void Enqueue(Guid guid)
        {
            EnqueueRange(guid.ToByteArray());
        }

        public Guid DequeueGuid()
        {
            return new Guid(DequeueRange(16));
        }

        public void Enqueue(Guid? guid)
        {
            EnqueueNullable(guid, Enqueue);
        }

        public Guid? DequeueNullableGuid()
        {
            return DequeueNullable(DequeueGuid);
        }

        public void Enqueue(IEnumerable<Guid> guids)
        {
            Enqueue(guids, Enqueue);
        }

        public Guid[] DequeueGuids()
        {
            return DequeueArray(DequeueGuid);
        }

        public void Enqueue(RequestSong value)
        {
            Enqueue(value.Song);
            EnqueueNullable(value.Position, Enqueue);
            Enqueue(value.Duration);
        }

        public RequestSong DequeueRequestSong()
        {
            Song song = DequeueSong();
            TimeSpan? position = DequeueNullable(DequeueTimeSpan);
            TimeSpan duration = DequeueTimeSpan();

            return RequestSong.Get(song, position, duration);
        }

        public void Enqueue(RequestSong? value)
        {
            EnqueueNullable(value, Enqueue);
        }

        public RequestSong? DequeueNullableRequestSong()
        {
            return DequeueNullable(DequeueRequestSong);
        }

        public void Enqueue(ISourcePlaylistBase playlist)
        {
            Enqueue((IPlaylistBase)playlist);
            Enqueue(playlist.FileMediaSources);
        }

        public ISourcePlaylistBase DequeueSourcePlaylist(Func<Guid, ISourcePlaylistBase> createFunc)
        {
            ISourcePlaylistBase playlist = createFunc(DequeueGuid());

            DequeuePlaylist(playlist);
            playlist.FileMediaSources = DequeueFileMediaSources();

            return playlist;
        }

        public void Enqueue(FileMediaSource fileMediaSource)
        {
            Enqueue(fileMediaSource.RootId);
            Enqueue(fileMediaSource.RelativePath);
        }

        public FileMediaSource DequeueFileMediaSource()
        {
            return new FileMediaSource()
            {
                RootId = DequeueGuid(),
                RelativePath = DequeueString(),
            };
        }

        public void Enqueue(IEnumerable<FileMediaSource> fileMediaSources)
        {
            Enqueue(fileMediaSources, Enqueue);
        }

        public FileMediaSource[] DequeueFileMediaSources()
        {
            return DequeueArray(DequeueFileMediaSource);
        }

        public void Enqueue(FileMediaSourceRoot fileMediaSourceRoot)
        {
            Enqueue(fileMediaSourceRoot.Id);
            Enqueue(fileMediaSourceRoot.Name);
        }

        public FileMediaSourceRoot DequeueFileMediaSourceRoot(IEnumerable<FileMediaSourceRoot> currentRoots)
        {
            Guid id = DequeueGuid();
            FileMediaSourceRoot currentRoot = currentRoots.ToNotNull().FirstOrDefault(root => root.Id == id);
            return new FileMediaSourceRoot()
            {
                Id = id,
                Name = DequeueString(),
                Type = currentRoot.Type,
                Value = currentRoot.Value,
            };
        }

        public void Enqueue(IEnumerable<FileMediaSourceRoot> fileMediaSourceRoots)
        {
            Enqueue(fileMediaSourceRoots, Enqueue);
        }

        public FileMediaSourceRoot[] DequeueFileMediaSourceRoots(IEnumerable<FileMediaSourceRoot> currentRoots)
        {
            return DequeueArray(() => DequeueFileMediaSourceRoot(currentRoots));
        }

        public void Enqueue(IPlaylistBase playlist)
        {
            Enqueue(playlist.ID);
            Enqueue(playlist.CurrentSong);
            Enqueue(playlist.Songs);
            Enqueue(playlist.Name);
            Enqueue(playlist.Duration);
            Enqueue((int)playlist.Shuffle);
            Enqueue((int)playlist.Loop);
            Enqueue(playlist.Position);
            Enqueue(playlist.WannaSong);
        }

        public IPlaylistBase DequeuePlaylist(Func<Guid, IPlaylistBase> createFunc)
        {
            return DequeuePlaylist(createFunc(DequeueGuid()));
        }

        public IPlaylistBase DequeuePlaylist(IPlaylistBase playlist)
        {
            playlist.CurrentSong = DequeueNullableSong();
            playlist.Songs = DequeueSongs();
            playlist.Name = DequeueString();
            playlist.Duration = DequeueTimeSpan();
            playlist.Shuffle = (OrderType)DequeueInt();
            playlist.Loop = (LoopType)DequeueInt();
            playlist.Position = DequeueTimeSpan();
            playlist.WannaSong = DequeueNullableRequestSong();

            return playlist;
        }

        public void Enqueue(IEnumerable<ISourcePlaylistBase> playlists)
        {
            Enqueue(playlists, Enqueue);
        }

        public ISourcePlaylistBase[] DequeueSourcePlaylists(Func<Guid, ISourcePlaylistBase> createFunc)
        {
            return DequeueArray(() => DequeueSourcePlaylist(createFunc));
        }

        public void Enqueue(IEnumerable<IPlaylistBase> playlists)
        {
            Enqueue(playlists, Enqueue);
        }

        public IPlaylistBase[] DequeuePlaylists(Func<Guid, IPlaylistBase> createFunc)
        {
            return DequeueArray(() => DequeuePlaylist(createFunc));
        }

        private void EnqueueNullable<T>(T? value, Action<T> valueEnqueueAction) where T : struct
        {
            Enqueue(value.HasValue);

            if (value.HasValue) valueEnqueueAction(value.Value);
        }

        private T? DequeueNullable<T>(Func<T> itemDequeueFunc) where T : struct
        {
            return DequeueBool() ? (T?)itemDequeueFunc() : null;
        }

        private void EnqueueClass<T>(T value, Action<T> valueEnqueueAction)
        {
            if (value == null) Enqueue(false);
            else
            {
                Enqueue(true);
                valueEnqueueAction(value);
            }
        }

        private T DequeueOrDefault<T>(Func<T> itemDequeueFunc)
        {
            return DequeueBool() ? itemDequeueFunc() : default(T);
        }

        public void Enqueue(IAudioServiceBase service)
        {
            Enqueue(service.FileMediaSourceRoots);
            Enqueue(service.SourcePlaylists);
            Enqueue(service.Playlists);
            Enqueue(service.CurrentPlaylist?.ID ?? Guid.Empty);
            Enqueue(service.Volume);
            Enqueue((int)service.PlayState);
        }

        public void DequeueService(IAudioServiceBase service,
            Func<Guid, ISourcePlaylistBase> createSourcePlaylistFunc, Func<Guid, IPlaylistBase> createPlaylistFunc)
        {
            service.FileMediaSourceRoots = DequeueFileMediaSourceRoots(service.FileMediaSourceRoots);
            service.SourcePlaylists = DequeueSourcePlaylists(createSourcePlaylistFunc);
            service.Playlists = DequeuePlaylists(createPlaylistFunc);

            Guid currentPlaylistId = DequeueGuid();
            //service.CurrentPlaylist = service.GetAllPlaylists()
                //.FirstOrDefault(p => p.ID == currentPlaylistId) ?? service.GetAllPlaylists().FirstOrDefault();

            service.Volume = DequeueFloat();
            service.PlayState = (PlaybackState)DequeueInt();
        }

        private void Enqueue<T>(IEnumerable<T> items, Action<T> itemEnqueueAction)
        {
            IList<T> list = items as IList<T> ?? items?.ToArray();

            if (list != null)
            {
                Enqueue(list.Count);
                foreach (T item in list) itemEnqueueAction(item);
            }
            else Enqueue(-1);
        }

        private T[] DequeueArray<T>(Func<T> itemDequeueFunc)
        {
            int length = DequeueInt();
            if (length == -1) return null;

            T[] array = new T[length];

            for (int i = 0; i < length; i++)
            {
                array[i] = itemDequeueFunc();
            }

            return array;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return bytes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return bytes.GetEnumerator();
        }

        public static implicit operator byte[](ByteQueue queue)
        {
            return queue.ToArray();
        }

        public static implicit operator ByteQueue(byte[] bytes)
        {
            return new ByteQueue(bytes);
        }
    }
}
