using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioPlayerBackend.Communication.MQTT
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

        public void Enqueue(bool value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
        }

        public void Enqueue(ushort value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
        }

        public void Enqueue(int value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
        }

        public void Enqueue(float value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
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

        public void Enqueue(Song song)
        {
            Enqueue(song.Index);
            Enqueue(song.Artist);
            Enqueue(song.FullPath);
            Enqueue(song.Title);
        }

        public void Enqueue(IEnumerable<Song> songs)
        {
            Enqueue(songs, Enqueue);
        }

        public void Enqueue(TimeSpan span)
        {
            EnqueueRange(BitConverter.GetBytes(span.Ticks));
        }

        public void Enqueue(WaveFormat format)
        {
            Enqueue(format.AverageBytesPerSecond);
            Enqueue(format.BitsPerSample);
            Enqueue(format.BlockAlign);
            Enqueue(format.Channels);
            Enqueue((ushort)format.Encoding);
            Enqueue(format.SampleRate);
        }

        public void Enqueue(Guid guid)
        {
            EnqueueRange(guid.ToByteArray());
        }

        public void Enqueue(IEnumerable<Guid> guids)
        {
            Enqueue(guids, Enqueue);
        }

        public void Enqueue(RequestSong value)
        {
            Enqueue(value.Song);
            Enqueue(value.Position);
            Enqueue(value.Duration);
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

        public byte[] DequeueRange(int count)
        {
            byte[] dequeueBytes = new byte[count];

            for (int i = 0; i < count; i++) dequeueBytes[i] = bytes.Dequeue();

            return dequeueBytes;
        }

        public bool DequeueBool()
        {
            return BitConverter.ToBoolean(DequeueRange(sizeof(bool)), 0);
        }

        public ushort DequeueUShort()
        {
            return BitConverter.ToUInt16(DequeueRange(sizeof(ushort)), 0);
        }

        public int DequeueInt()
        {
            return BitConverter.ToInt32(DequeueRange(sizeof(int)), 0);
        }

        public float DequeueFloat()
        {
            return BitConverter.ToSingle(DequeueRange(sizeof(float)), 0);
        }

        public string DequeueString()
        {
            int length = DequeueInt();
            return length >= 0 ? Encoding.UTF8.GetString(DequeueRange(length)) : null;
        }

        public string[] DequeueStrings()
        {
            return DequeueArray(DequeueString);
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

        public Song[] DequeueSongs()
        {
            return DequeueArray(DequeueSong);
        }

        public TimeSpan DequeueTimeSpan()
        {
            return TimeSpan.FromTicks(BitConverter.ToInt64(DequeueRange(8), 0));
        }

        public WaveFormat DequeueWaveFormat()
        {
            int averageBytesPerSecond = DequeueInt();
            int bitsPerSample = DequeueInt();
            int blockAlign = DequeueInt();
            int channels = DequeueInt();
            WaveFormatEncoding encoding = (WaveFormatEncoding)DequeueUShort();
            int sampleRate = DequeueInt();

            return new WaveFormat(encoding, sampleRate,
                channels, averageBytesPerSecond, blockAlign, bitsPerSample);
        }

        public Guid DequeueGuid()
        {
            return new Guid(DequeueRange(16));
        }

        public Guid[] DequeueGuids()
        {
            return DequeueArray(DequeueGuid);
        }

        public RequestSong DequeueSongWithPosition()
        {
            Song song = DequeueSong();
            TimeSpan postion = DequeueTimeSpan();
            TimeSpan duration = DequeueTimeSpan();

            return RequestSong.Get(song, postion, duration);
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

        public static implicit operator byte[] (ByteQueue queue)
        {
            return queue.ToArray();
        }

        public static implicit operator ByteQueue(byte[] bytes)
        {
            return new ByteQueue(bytes);
        }
    }
}
