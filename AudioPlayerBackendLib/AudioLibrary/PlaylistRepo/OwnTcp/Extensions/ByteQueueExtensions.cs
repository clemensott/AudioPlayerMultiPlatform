using AudioPlayerBackend.Communication.Base;
using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions
{
    internal static class ByteQueueExtensions
    {
        public static void Enqueue(this ByteQueue queue, PlaylistType playlistType)
        {
            queue.Enqueue((int)playlistType);
        }

        public static PlaylistType DequeuePlaylistType(this ByteQueue queue)
        {
            return (PlaylistType)queue.DequeueInt();
        }

        public static void Enqueue(this ByteQueue queue, Song song)
        {
            queue.Enqueue(song.Index);
            queue.Enqueue(song.Artist);
            queue.Enqueue(song.FullPath);
            queue.Enqueue(song.Title);
        }

        public static Song DequeueSong(this ByteQueue queue)
        {
            return new Song()
            {
                Index = queue.DequeueInt(),
                Artist = queue.DequeueString(),
                FullPath = queue.DequeueString(),
                Title = queue.DequeueString()
            };
        }

        public static void Enqueue(this ByteQueue queue, Song? song)
        {
            queue.EnqueueNullable(song, queue.Enqueue);
        }

        public static Song? DequeueNullableSong(this ByteQueue queue)
        {
            return queue.DequeueNullable(queue.DequeueSong);
        }

        public static void Enqueue(this ByteQueue queue, IEnumerable<Song> songs)
        {
           queue. Enqueue(songs, queue.Enqueue);
        }

        public static Song[] DequeueSongs(this ByteQueue queue)
        {
            return queue.DequeueArray(queue.DequeueSong);
        }
    }
}
