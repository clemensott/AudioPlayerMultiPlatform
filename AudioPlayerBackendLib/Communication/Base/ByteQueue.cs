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

        public ByteQueue EnqueueRange(IEnumerable<byte> enqueueBytes)
        {
            foreach (byte item in enqueueBytes) bytes.Enqueue(item);
            return this;
        }

        public byte[] DequeueRange(int count)
        {
            byte[] dequeueBytes = new byte[count];

            for (int i = 0; i < count; i++) dequeueBytes[i] = bytes.Dequeue();

            return dequeueBytes;
        }

        public ByteQueue Enqueue(bool value)
        {
            return EnqueueRange(BitConverter.GetBytes(value));
        }

        public bool DequeueBool()
        {
            return BitConverter.ToBoolean(DequeueRange(sizeof(bool)), 0);
        }

        public ByteQueue Enqueue(ushort value)
        {
            return EnqueueRange(BitConverter.GetBytes(value));
        }

        public ushort DequeueUShort()
        {
            return BitConverter.ToUInt16(DequeueRange(sizeof(ushort)), 0);
        }

        public ByteQueue Enqueue(int value)
        {
            return EnqueueRange(BitConverter.GetBytes(value));
        }

        public int DequeueInt()
        {
            return BitConverter.ToInt32(DequeueRange(sizeof(int)), 0);
        }

        public ByteQueue Enqueue(int? value)
        {
            return EnqueueNullable(value, Enqueue);
        }

        public int? DequeueIntNullable()
        {
            return DequeueNullable(DequeueInt);
        }

        public ByteQueue Enqueue(long value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
            return this;
        }

        public long DequeueLong()
        {
            return BitConverter.ToInt64(DequeueRange(sizeof(long)), 0);
        }

        public float DequeueFloat()
        {
            return BitConverter.ToSingle(DequeueRange(sizeof(float)), 0);
        }

        public ByteQueue Enqueue(float value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
            return this;
        }

        public double DequeueDouble()
        {
            return BitConverter.ToDouble(DequeueRange(sizeof(double)), 0);
        }

        public ByteQueue Enqueue(double value)
        {
            EnqueueRange(BitConverter.GetBytes(value));
            return this;
        }

        public string DequeueString()
        {
            int length = DequeueInt();
            return length >= 0 ? Encoding.UTF8.GetString(DequeueRange(length)) : null;
        }

        public ByteQueue Enqueue(string value)
        {
            if (value == null) Enqueue(-1);
            else
            {
                byte[] valueBytes = Encoding.UTF8.GetBytes(value);

                Enqueue(valueBytes.Length);
                EnqueueRange(valueBytes);
            }
            return this;
        }

        public ByteQueue Enqueue(IEnumerable<string> strings)
        {
            return Enqueue(strings, Enqueue);
        }

        public string[] DequeueStrings()
        {
            return DequeueArray(DequeueString);
        }

        public ByteQueue Enqueue(DateTime? value)
        {
            return EnqueueNullable(value, Enqueue);
        }

        public DateTime? DequeueDateTimeNullable()
        {
            return DequeueNullable(DequeueDateTime);
        }

        public ByteQueue Enqueue(DateTime value)
        {
            return Enqueue(value.Ticks);
        }

        public DateTime DequeueDateTime()
        {
            return new DateTime(DequeueLong());
        }

        public ByteQueue Enqueue(TimeSpan span)
        {
            return Enqueue(span.Ticks);
        }

        public TimeSpan DequeueTimeSpan()
        {
            return TimeSpan.FromTicks(DequeueLong());
        }

        public ByteQueue Enqueue(TimeSpan? span)
        {
            return EnqueueNullable(span, Enqueue);
        }

        public TimeSpan? DequeueTimeSpanNullable()
        {
            return DequeueNullable(DequeueTimeSpan);
        }

        public ByteQueue Enqueue(Guid guid)
        {
            return EnqueueRange(guid.ToByteArray());
        }

        public Guid DequeueGuid()
        {
            return new Guid(DequeueRange(16));
        }

        public ByteQueue Enqueue(Guid? guid)
        {
            return EnqueueNullable(guid, Enqueue);
        }

        public Guid? DequeueGuidNullable()
        {
            return DequeueNullable(DequeueGuid);
        }

        public ByteQueue Enqueue(IEnumerable<Guid> guids)
        {
            return Enqueue(guids, Enqueue);
        }

        public Guid[] DequeueGuids()
        {
            return DequeueArray(DequeueGuid);
        }

        public ByteQueue EnqueueNullable<T>(T? value, Func<T, ByteQueue> valueEnqueueAction) where T : struct
        {
            return EnqueueNullable(value, v =>
            {
                valueEnqueueAction(v);
            });
        }

        public ByteQueue EnqueueNullable<T>(T? value, Action<T> valueEnqueueAction) where T : struct
        {
            Enqueue(value.HasValue);

            if (value.HasValue) valueEnqueueAction(value.Value);
            return this;
        }

        public T? DequeueNullable<T>(Func<T> itemDequeueFunc) where T : struct
        {
            return DequeueBool() ? (T?)itemDequeueFunc() : null;
        }

        private ByteQueue EnqueueClass<T>(T value, Func<T, ByteQueue> valueEnqueueAction) where T : class
        {
            return EnqueueClass(value, v =>
            {
                valueEnqueueAction(v);
            });
        }

        private ByteQueue EnqueueClass<T>(T value, Action<T> valueEnqueueAction) where T : class
        {
            if (value == null) Enqueue(false);
            else
            {
                Enqueue(true);
                valueEnqueueAction(value);
            }

            return this;
        }

        private T DequeueOrDefault<T>(Func<T> itemDequeueFunc)
        {
            return DequeueBool() ? itemDequeueFunc() : default(T);
        }

        public ByteQueue Enqueue<T>(IEnumerable<T> items, Func<T, ByteQueue> itemEnqueueAction)
        {
            return Enqueue(items, v =>
            {
                itemEnqueueAction(v);
            });
        }

        public ByteQueue Enqueue<T>(IEnumerable<T> items, Action<T> itemEnqueueAction)
        {
            IList<T> list = items as IList<T> ?? items?.ToArray();

            if (list != null)
            {
                Enqueue(list.Count);
                foreach (T item in list) itemEnqueueAction(item);
            }
            else Enqueue(-1);

            return this;
        }

        public T[] DequeueArray<T>(Func<T> itemDequeueFunc)
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
            return bytes == null ? null : new ByteQueue(bytes);
        }
    }
}
