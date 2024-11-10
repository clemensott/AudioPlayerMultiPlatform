using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Communication.Base;
using System;
using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions
{
    public static class ByteQueueExtensions
    {
        public static ByteQueue Enqueue(this ByteQueue queue, PlaylistType playlistType)
        {
            return queue.Enqueue((int)playlistType);
        }

        public static PlaylistType DequeuePlaylistType(this ByteQueue queue)
        {
            return (PlaylistType)queue.DequeueInt();
        }

        public static ByteQueue Enqueue(this ByteQueue queue, OrderType orderType)
        {
            return queue.Enqueue((int)orderType);
        }

        public static OrderType DequeueOrderType(this ByteQueue queue)
        {
            return (OrderType)queue.DequeueInt();
        }

        public static ByteQueue Enqueue(this ByteQueue queue, LoopType loopType)
        {
            return queue.Enqueue((int)loopType);
        }

        public static LoopType DequeueLoopType(this ByteQueue queue)
        {
            return (LoopType)queue.DequeueInt();
        }

        public static ByteQueue Enqueue(this ByteQueue queue, Song song)
        {
            return queue
                .Enqueue(song.Id)
                .Enqueue(song.Index)
                .Enqueue(song.Artist)
                .Enqueue(song.FullPath)
                .Enqueue(song.Title);
        }

        public static Song DequeueSong(this ByteQueue queue)
        {
            return new Song()
            {
                Id = queue.DequeueGuid(),
                Index = queue.DequeueInt(),
                Artist = queue.DequeueString(),
                FullPath = queue.DequeueString(),
                Title = queue.DequeueString()
            };
        }

        public static ByteQueue Enqueue(this ByteQueue queue, Song? song)
        {
            return queue.EnqueueNullable(song, queue.Enqueue);
        }

        public static Song? DequeueNullableSong(this ByteQueue queue)
        {
            return queue.DequeueNullable(queue.DequeueSong);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, IEnumerable<Song> songs)
        {
            return queue.Enqueue(songs, queue.Enqueue);
        }

        public static Song[] DequeueSongs(this ByteQueue queue)
        {
            return queue.DequeueArray(queue.DequeueSong);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, RequestSong song)
        {
            return queue
                .Enqueue(song.Song)
                .Enqueue(song.Position)
                .Enqueue(song.Duration)
                .Enqueue(song.ContinuePlayback);
        }

        public static RequestSong DequeueRequestSong(this ByteQueue queue)
        {
            Song song = queue.DequeueSong();
            TimeSpan position = queue.DequeueTimeSpan();
            TimeSpan duration = queue.DequeueTimeSpan();
            bool continuePlayback = queue.DequeueBool();

            return RequestSong.Get(song, position, duration, continuePlayback);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, RequestSong? song)
        {
            return queue.EnqueueNullable(song, queue.Enqueue);
        }

        public static RequestSong? DequeueRequestSongNullable(this ByteQueue queue)
        {
            return queue.DequeueNullable(queue.DequeueRequestSong);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, FileMediaSourceRootUpdateType updateType)
        {
            return queue.Enqueue((int)updateType);
        }

        public static FileMediaSourceRootUpdateType DequeueFileMediaSourceRootUpdateType(this ByteQueue queue)
        {
            return (FileMediaSourceRootUpdateType)queue.DequeueInt();
        }

        public static ByteQueue Enqueue(this ByteQueue queue, FileMediaSourceRootPathType pathType)
        {
            return queue.Enqueue((int)pathType);
        }

        public static FileMediaSourceRootPathType DequeueFileMediaSourceRootPathType(this ByteQueue queue)
        {
            return (FileMediaSourceRootPathType)queue.DequeueInt();
        }

        public static ByteQueue Enqueue(this ByteQueue queue, FileMediaSourceRoot fileMediaSourceRoot)
        {
            return queue
                .Enqueue(fileMediaSourceRoot.Id)
                .Enqueue(fileMediaSourceRoot.UpdateType)
                .Enqueue(fileMediaSourceRoot.Name)
                .Enqueue(fileMediaSourceRoot.PathType)
                .Enqueue(fileMediaSourceRoot.Path);
        }

        public static FileMediaSourceRoot DequeueFileMediaSourceRoot(this ByteQueue queue)
        {
            Guid id = queue.DequeueGuid();
            FileMediaSourceRootUpdateType updateType = queue.DequeueFileMediaSourceRootUpdateType();
            string name = queue.DequeueString();
            FileMediaSourceRootPathType pathType = queue.DequeueFileMediaSourceRootPathType();
            string path = queue.DequeueString();

            return new FileMediaSourceRoot(id, updateType, name, pathType, path);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, FileMediaSourceRootInfo fileMediaSourceRoot)
        {
            return queue
                .Enqueue(fileMediaSourceRoot.UpdateType)
                .Enqueue(fileMediaSourceRoot.Name)
                .Enqueue(fileMediaSourceRoot.PathType)
                .Enqueue(fileMediaSourceRoot.Path);
        }

        public static FileMediaSourceRootInfo DequeueFileMediaSourceRootInfo(this ByteQueue queue)
        {
            FileMediaSourceRootUpdateType updateType = queue.DequeueFileMediaSourceRootUpdateType();
            string name = queue.DequeueString();
            FileMediaSourceRootPathType pathType = queue.DequeueFileMediaSourceRootPathType();
            string path = queue.DequeueString();

            return new FileMediaSourceRootInfo(updateType, name, pathType, path);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, ICollection<FileMediaSourceRootInfo> fileMediaSourceRoots)
        {
            return queue.Enqueue(fileMediaSourceRoots, queue.Enqueue);
        }

        public static ICollection<FileMediaSourceRootInfo> DequeueFileMediaSourceRootInfos(this ByteQueue queue)
        {
            return queue.DequeueArray(queue.DequeueFileMediaSourceRootInfo);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, ICollection<FileMediaSourceRoot> fileMediaSourceRoots)
        {
            return queue.Enqueue(fileMediaSourceRoots, queue.Enqueue);
        }

        public static ICollection<FileMediaSourceRoot> DequeueFileMediaSourceRoots(this ByteQueue queue)
        {
            return queue.DequeueArray(queue.DequeueFileMediaSourceRoot);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, FileMediaSource fileMediaSource)
        {
            return queue.Enqueue(fileMediaSource.RelativePath);
        }

        public static FileMediaSource DequeueFileMediaSource(this ByteQueue queue)
        {
            string relativePath = queue.DequeueString();
            return new FileMediaSource(relativePath);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, IEnumerable<FileMediaSource> fileMediaSources)
        {
            return queue.Enqueue(fileMediaSources, queue.Enqueue);
        }

        public static FileMediaSource[] DequeueFileMediaSourceArray(this ByteQueue queue)
        {
            return queue.DequeueArray(queue.DequeueFileMediaSource);
        }

        public static ByteQueue Enqueue(this ByteQueue queue, FileMediaSources fileMediaSources)
        {
            return queue.EnqueueClass(fileMediaSources, _ =>
            {
                queue
                    .Enqueue(fileMediaSources.Root)
                    .Enqueue(fileMediaSources.Sources);
            });
        }

        public static FileMediaSources DequeueFileMediaSources(this ByteQueue queue)
        {
            return queue.DequeueOrDefault(() =>
            {
                FileMediaSourceRoot root = queue.DequeueFileMediaSourceRoot();
                FileMediaSource[] sources = queue.DequeueFileMediaSourceArray();

                return new FileMediaSources(root, sources);
            });
        }

        public static ByteQueue Enqueue(this ByteQueue queue, Playlist playlist)
        {
            return queue.Enqueue(playlist.Id)
                .Enqueue(playlist.Type)
                .Enqueue(playlist.Name)
                .Enqueue(playlist.Shuffle)
                .Enqueue(playlist.Loop)
                .Enqueue(playlist.PlaybackRate)
                .Enqueue(playlist.Position)
                .Enqueue(playlist.Duration)
                .Enqueue(playlist.RequestSong)
                .Enqueue(playlist.CurrentSongId)
                .Enqueue(playlist.Songs)
                .Enqueue(playlist.FileMediaSources)
                .Enqueue(playlist.NextPlaylist)
                .Enqueue(playlist.FilesLastUpdated)
                .Enqueue(playlist.SongsLastUpdated);
        }

        public static Playlist DequeuePlaylist(this ByteQueue queue)
        {
            Guid id = queue.DequeueGuid();
            PlaylistType type = queue.DequeuePlaylistType();
            string name = queue.DequeueString();
            OrderType shuffle = queue.DequeueOrderType();
            LoopType loop = queue.DequeueLoopType();
            double playbackRate = queue.DequeueDouble();
            TimeSpan position = queue.DequeueTimeSpan();
            TimeSpan duration = queue.DequeueTimeSpan();
            RequestSong? requestSong = queue.DequeueRequestSongNullable();
            Guid? currentSongId = queue.DequeueGuidNullable();
            Song[] songs = queue.DequeueSongs();
            FileMediaSources fileMediaSources = queue.DequeueFileMediaSources();
            Guid? nextPlaylist = queue.DequeueGuidNullable();
            DateTime? filesLastUpdated = queue.DequeueDateTimeNullable();
            DateTime? songsLastUpdated = queue.DequeueDateTimeNullable();

            return new Playlist(id, type, name, shuffle, loop, playbackRate, position, duration,
                requestSong, currentSongId, songs, fileMediaSources, nextPlaylist, filesLastUpdated, songsLastUpdated);
        }
    }
}
