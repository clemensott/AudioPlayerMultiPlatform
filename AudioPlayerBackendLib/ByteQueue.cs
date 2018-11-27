using AudioPlayerBackend.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioPlayerBackend
{
    class ByteQueue : IEnumerable<byte>
    {
        private Queue<byte> bytes;

        public ByteQueue()
        {
            bytes = new Queue<byte>();
        }

        public ByteQueue(IEnumerable<byte> bytes)
        {
            this.bytes = new Queue<byte>(bytes);
        }

        private void EnqueueRange(IEnumerable<byte> bytes)
        {
            foreach (byte item in bytes) this.bytes.Enqueue(item);
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
                byte[] bytes = Encoding.UTF8.GetBytes(value);

                Enqueue(bytes.Length);
                EnqueueRange(bytes);
            }
        }

        public void Enqueue(IEnumerable<string> strings)
        {
            IList<string> list = strings is IList<string> ? (IList<string>)strings : strings.ToArray();

            Enqueue(list.Count);
            foreach (string song in list) Enqueue(song);
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
            IList<Song> list = songs is IList<Song> ? (IList<Song>)songs : songs.ToArray();

            Enqueue(list.Count);
            foreach (Song song in list) Enqueue(song);
        }

        public void Enqueue(TimeSpan span)
        {
            EnqueueRange(BitConverter.GetBytes(span.Ticks));
        }

        public void Enqueue(PlaybackState state)
        {
            Enqueue((int)state);
        }

        public void Enqueue(WaveFormatEncoding encoding)
        {
            Enqueue((ushort)encoding);
        }

        public void Enqueue(WaveFormat format)
        {
            Enqueue(format.AverageBytesPerSecond);
            Enqueue(format.BitsPerSample);
            Enqueue(format.BlockAlign);
            Enqueue(format.Channels);
            Enqueue(format.Encoding);
            Enqueue(format.SampleRate);
        }

        //private void Enqueue(Hashes hashes)
        //{
        //    Enqueue(hashes.AllSongsHash);
        //    Enqueue(hashes.CurrentSongHash);
        //    Enqueue(hashes.MediaSourcesHash);
        //    Enqueue(hashes.SearchSongsHash);
        //}

        //public void Enqueue(States states)
        //{
        //    Enqueue(states.Duration);
        //    Enqueue(states.Hashes);
        //    Enqueue(states.IsAllShuffle);
        //    Enqueue(states.IsOnlySearch);
        //    Enqueue(states.IsSearchShuffle);
        //    Enqueue(states.PlayState);
        //    Enqueue(states.Position);
        //    Enqueue(states.SearchKey);
        //}

        public byte[] DequeueRange(int count)
        {
            List<byte> bytes = new List<byte>();

            for (int i = 0; i < count; i++) bytes.Add(this.bytes.Dequeue());

            return bytes.ToArray();
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
            int length = DequeueInt();
            string[] strings = new string[length];

            for (int i = 0; i < length; i++) strings[i] = DequeueString();

            return strings;
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
            int length = DequeueInt();
            Song[] songs = new Song[length];

            for (int i = 0; i < length; i++) songs[i] = DequeueSong();

            return songs;
        }

        public TimeSpan DequeueTimeSpan()
        {
            return TimeSpan.FromTicks(BitConverter.ToInt64(DequeueRange(8), 0));
        }

        public PlaybackState DequeuePlayState()
        {
            return (PlaybackState)DequeueInt();
        }

        public WaveFormatEncoding DequeueWaveFormatEncoding()
        {
            return (WaveFormatEncoding)DequeueUShort();
        }

        public WaveFormat DequeueWaveFormat()
        {
            int averageBytesPerSecond = DequeueInt();
            int bitsPerSample = DequeueInt();
            int blockAlign = DequeueInt();
            int channels = DequeueInt();
            WaveFormatEncoding encoding = DequeueWaveFormatEncoding();
            int sampleRate = DequeueInt();

            return new WaveFormat(encoding, sampleRate,
                channels, averageBytesPerSecond, blockAlign, bitsPerSample);
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
