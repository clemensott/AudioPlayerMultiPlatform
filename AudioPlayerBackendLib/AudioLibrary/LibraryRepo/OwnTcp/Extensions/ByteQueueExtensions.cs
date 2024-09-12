using AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp.Extensions
{
    internal static class ByteQueueExtensions
    {
        public static ByteQueue Enqueue(this ByteQueue queue, PlaybackState playState)
        {
            return queue.Enqueue((int)playState);
        }

        public static PlaybackState DequeuePlaybackState(this ByteQueue queue)
        {
            return (PlaybackState)queue.DequeueInt();
        }

        public static ByteQueue Enqueue(this ByteQueue queue, PlaylistInfo playlist)
        {
            return queue
                .Enqueue(playlist.Id)
                .Enqueue(playlist.Type)
                .Enqueue(playlist.Name)
                .Enqueue(playlist.SongsCount)
                .Enqueue(playlist.FilesLastUpdated)
                .Enqueue(playlist.SongsLastUpdated);
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

        public static ByteQueue Enqueue(this ByteQueue queue, IEnumerable<PlaylistInfo> playlists)
        {
            return queue.Enqueue(playlists, queue.Enqueue);
        }

        public static PlaylistInfo[] DequeuePlaylistInfos(this ByteQueue queue)
        {
            return queue.DequeueArray(queue.DequeuePlaylistInfo);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, Library library)
        {
            return queue
                .Enqueue(library.PlayState)
                .Enqueue(library.Volume)
                .Enqueue(library.CurrentPlaylistId)
                .Enqueue(library.Playlists)
                .Enqueue(library.FoldersLastUpdated);
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
