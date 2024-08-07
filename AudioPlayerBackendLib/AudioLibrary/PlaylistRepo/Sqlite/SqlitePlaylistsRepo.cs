using AudioPlayerBackend.AudioLibrary.Database.Sql;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.Sqlite
{
    internal class SqlitePlaylistsRepo : BaseSqlRepo, IPlaylistsRepo
    {
        public event EventHandler<PlaylistChangeArgs<string>> OnNameChange;
        public event EventHandler<PlaylistChangeArgs<OrderType>> OnShuffleChange;
        public event EventHandler<PlaylistChangeArgs<LoopType>> OnLoopChange;
        public event EventHandler<PlaylistChangeArgs<double>> OnPlaybackRateChange;
        public event EventHandler<PlaylistChangeArgs<TimeSpan>> OnPositionChange;
        public event EventHandler<PlaylistChangeArgs<TimeSpan>> OnDurationChange;
        public event EventHandler<PlaylistChangeArgs<RequestSong?>> OnRequestSongChange;
        public event EventHandler<PlaylistChangeArgs<Guid?>> OnCurrentSongIdChange;
        public event EventHandler<PlaylistChangeArgs<ICollection<Song>>> OnSongsChange;
        public event EventHandler<InsertPlaylistArgs> OnInsertPlaylist;
        public event EventHandler<RemovePlaylistArgs> OnRemovePlaylist;
        public event EventHandler<PlaylistChangeArgs<FileMediaSources>> OnFileMedisSourcesChange;

        public SqlitePlaylistsRepo(ISqlExecuteService sqlExecuteService) : base(sqlExecuteService)
        {
        }

        public async Task Init()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS playlists
                (
                    id                        TEXT PRIMARY KEY,
                    type                      INTEGER NOT NULL,
                    name                      TEXT    NOT NULL,
                    shuffle                   INTEGER NOT NULL,
                    loop                      INTEGER NOT NULL,
                    playback_rate             REAL    NOT NULL,
                    position                  INTEGER NOT NULL,
                    duration                  INTEGER NOT NULL,
                    current_song_id           TEXT,
                    requested_song_id         TEXT,
                    requested_song_index      INTEGER,
                    requested_song_title      TEXT,
                    requested_song_artist     TEXT,
                    requested_song_full_path  TEXT,
                    requested_song_position   INTEGER,
                    requested_song_duration   INTEGER,
                    file_media_source_root_id TEXT,
                    created                   TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ";
        }

        public async Task<Playlist> GetPlaylist(Guid playlistId)
        {
            string playlistSql = $@"
                SELECT id, type, name, shuffle, loop, playback_rate, position, duration, current_song_id,
                    requested_song_id, requested_song_index, requested_song_title, requested_song_artist,
                    requested_song_full_path, requested_song_position, requested_song_duration,
                    file_media_source_root_id
                FROM playlists
                WHERE id = @id;
            ";
            var playlistParameters = CreateParams("id", playlistId.ToString());
            var playlist = await sqlExecuteService.ExecuteReadFirstAsync(reader =>
            {
                Guid id = reader.GetGuidFromString("id");
                PlaylistType type = (PlaylistType)reader.GetInt64("type");
                string name = reader.GetString("name");
                OrderType shuffle = (OrderType)reader.GetInt64("shuffle");
                LoopType loop = (LoopType)reader.GetInt64("loop");
                double playbackRate = reader.GetDouble("playback_rate");
                TimeSpan position = reader.GetTimespanFromInt64("position");
                TimeSpan duration = reader.GetTimespanFromInt64("duration");
                Guid? currentSongId = reader.GetGuidNullableFromString("current_song_id");
                RequestSong? requestedSong = GetRequestedSong(reader);
                Guid? fileMediaSourceRootId = reader.GetGuidNullableFromString("file_media_source_root_id");

                return (id, type, name, shuffle, loop, playbackRate, position, duration,
                    currentSongId, requestedSong, fileMediaSourceRootId);
            }, playlistSql, playlistParameters);

            const string songsSql = @"
                SELECT id, index_value, title, artist, full_path
                FROM songs
                WHERE playlist_id = @playlistId
                ORDER BY index_value;
            ";
            var songsParameters = CreateParams("playlistId", playlistId.ToString());
            IList<Song> songs = await sqlExecuteService.ExecuteReadAllAsync(GetSong, songsSql, songsParameters);

            FileMediaSources fileMediaSources = null;
            if (playlist.fileMediaSourceRootId.HasValue)
            {
                const string fileMediaSourceRootSql = @"
                    SELECT id, update_type, name, path_type, path
                    FROM file_media_source_roots
                    WHERE id = @fileMediaSourceRoot;
                ";
                var fileMediaSourceRootParameter = CreateParams("fileMediaSourceRoot", playlist.fileMediaSourceRootId.Value.ToString());
                FileMediaSourceRoot fileMediaSourceRoot = await sqlExecuteService.ExecuteReadFirstAsync(GetFileMediaSourceRoot,
                    fileMediaSourceRootSql, fileMediaSourceRootParameter);

                const string fileMediaSourceEntriesSql = @"
                    SELECT relative_path
                    FROM file_media_sources
                    WHERE playlist_id = @playlistId;
                ";
                var fileMediaSourceEntriesParameter = CreateParams("playlistId", playlistId.ToString());
                IList<FileMediaSource> fileMediaSourceEntries = await sqlExecuteService.ExecuteReadAllAsync(GetFileMediaSource,
                    fileMediaSourceEntriesSql, fileMediaSourceEntriesParameter);

                fileMediaSources = new FileMediaSources(fileMediaSourceRoot, fileMediaSourceEntries);
            }

            return new Playlist(playlist.id, playlist.type, playlist.name, playlist.shuffle, playlist.loop, 
                playlist.playbackRate, playlist.position, playlist.duration, playlist.requestedSong,
                playlist.currentSongId, songs, fileMediaSources);

            RequestSong? GetRequestedSong(DbDataReader reader)
            {
                Guid? songId = reader.GetGuidNullableFromString("id");
                if (!songId.HasValue) return null;

                int songIndex = (int)reader.GetInt64("requested_song_index");
                string songTitle = reader.GetString("requested_song_index");
                string songArtist = reader.GetString("requested_song_artist");
                string songFullPath = reader.GetString("requested_song_full_path");
                TimeSpan requestPosition = reader.GetTimespanFromInt64("requested_song_position");
                TimeSpan requestDuration = reader.GetTimespanFromInt64("requested_song_duration");

                Song song = new Song(songId.Value, songIndex, songTitle, songArtist, songFullPath);
                return RequestSong.Get(song, requestPosition, requestDuration);
            }

            Song GetSong(DbDataReader reader)
            {
                Guid songId = reader.GetGuidFromString("id");
                int songIndex = (int)reader.GetInt64("index_value");
                string songTitle = reader.GetString("title");
                string songArtist = reader.GetStringNullable("artist");
                string songFullPath = reader.GetString("full_path");

                return new Song(songId, songIndex, songTitle, songArtist, songFullPath);
            }

            FileMediaSourceRoot GetFileMediaSourceRoot(DbDataReader reader)
            {
                Guid id = reader.GetGuidFromString("id");
                FileMediaSourceRootUpdateType updateType = (FileMediaSourceRootUpdateType)reader.GetInt64("update_type");
                string name = reader.GetString("name");
                FileMediaSourceRootPathType pathType = (FileMediaSourceRootPathType)reader.GetInt64("path_type");
                string path = reader.GetString("path");

                return new FileMediaSourceRoot(id, updateType, name, pathType, path);
            }

            FileMediaSource GetFileMediaSource(DbDataReader reader)
            {
                string relativePath = reader.GetString("relative_path");
                return new FileMediaSource(relativePath);
            }
        }

        public Task SendInsertPlaylist(Playlist playlist, int index)
        {
            const string playlistSql = @"
                INSERT INTO playlists ()
            ";
        }

        public Task SendRemovePlaylist(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task SendCurrentSongIdChange(Guid id, Guid currentSongId)
        {
            throw new NotImplementedException();
        }

        public Task SendCurrentSongIdChange(Guid id, Guid? currentSongId)
        {
            throw new NotImplementedException();
        }

        public Task SendDurationChange(Guid id, TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public Task SendFileMedisSourcesChange(Guid id, FileMediaSources fileMediaSources)
        {
            throw new NotImplementedException();
        }

        public Task SendLoopChange(Guid id, LoopType loop)
        {
            throw new NotImplementedException();
        }

        public Task SendNameChange(Guid id, string name)
        {
            throw new NotImplementedException();
        }

        public Task SendPlaybackRateChange(Guid id, double playbackRate)
        {
            throw new NotImplementedException();
        }

        public Task SendPositionChange(Guid id, TimeSpan position)
        {
            throw new NotImplementedException();
        }

        public Task SendRequestSongChange(Guid id, RequestSong requestSong)
        {
            throw new NotImplementedException();
        }

        public Task SendRequestSongChange(Guid id, RequestSong? requestSong)
        {
            throw new NotImplementedException();
        }

        public Task SendShuffleChange(Guid id, OrderType shuffle)
        {
            throw new NotImplementedException();
        }

        public Task SendSongsChange(Guid id, ICollection<Song> songs)
        {
            throw new NotImplementedException();
        }
    }
}
