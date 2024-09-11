using AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp.Extensions
{
    internal static class ByteQueueExtensions
    {
        public static void Enqueue(this ByteQueue queue, PlaybackState playState)
        {
            queue.Enqueue((int)playState);
        }

        public static PlaybackState DequeuePlaybackState(this ByteQueue queue)
        {
            return (PlaybackState)queue.DequeueInt();
        }

        public static void Enqueue(this ByteQueue queue, PlaylistInfo playlist)
        {
            queue.Enqueue(playlist.Id);
            queue.Enqueue(playlist.Type);
            queue.Enqueue(playlist.Name);
            queue.Enqueue(playlist.SongsCount);
            queue.Enqueue(playlist.FilesLastUpdated);
            queue.Enqueue(playlist.SongsLastUpdated);
        }

        public static PlaylistInfo DequeuePlaylistInfo(this ByteQueue queue)
        {
            Guid id = queue.DequeueGuid();
            PlaylistType type = queue.DequeuePlaylistType();
            string name = queue.DequeueString();
            int songsCount = queue.DequeueInt();
            DateTime? filesLastUpdated = queue.DequeueDateTimeNullable();
            DateTime? songsLastUpdated = queue.DequeueDateTimeNullable();

            return new PlaylistInfo(id, type, name, songsCount, filesLastUpdated, songsLastUpdated);
        }

        public static void Enqueue(this ByteQueue queue, IEnumerable<PlaylistInfo> playlists)
        {
            queue.Enqueue(playlists, queue.Enqueue);
        }

        public static PlaylistInfo[] DequeuePlaylistInfos(this ByteQueue queue)
        {
            return queue.DequeueArray(queue.DequeuePlaylistInfo);
        }

        public static void Enqueue(this ByteQueue queue, Library library)
        {
            queue.Enqueue(library.PlayState);
            queue.Enqueue(library.Volume);
            queue.Enqueue(library.CurrentPlaylistId);
            queue.Enqueue(library.Playlists);
            queue.Enqueue(library.FoldersLastUpdated);
        }

        public static Library DequeueLibrary(this ByteQueue queue)
        {
            PlaybackState playState = queue.DequeuePlaybackState();
            double volume = queue.DequeueDouble();
            Guid? currentPlaylistId = queue.DequeueGuidNullable();
            PlaylistInfo[] playlists = queue.DequeuePlaylistInfos();
            DateTime? foldersLastUpdated = queue.DequeueDateTimeNullable();

            return new Library(playState, volume, currentPlaylistId, playlists, foldersLastUpdated);
        }
    }
}
