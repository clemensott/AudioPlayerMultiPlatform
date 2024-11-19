using AudioPlayerBackend.AudioLibrary.Database.Sql;
using AudioPlayerBackend.AudioLibrary.Database.Sql.Extensions;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using StdOttStandard;
using StdOttStandard.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.Sqlite
{
    internal class SqlitePlaylistsRepo : BaseSqlRepo, IPlaylistsRepo
    {
        public event EventHandler<PlaylistChangeArgs<string>> NameChanged;
        public event EventHandler<PlaylistChangeArgs<OrderType>> ShuffleChanged;
        public event EventHandler<PlaylistChangeArgs<LoopType>> LoopChanged;
        public event EventHandler<PlaylistChangeArgs<double>> PlaybackRateChanged;
        public event EventHandler<PlaylistChangeArgs<SongRequest?>> CurrentSongRequestChanged;
        public event EventHandler<PlaylistChangeArgs<ICollection<Song>>> SongsChanged;
        public event EventHandler<InsertPlaylistArgs> InsertedPlaylist;
        public event EventHandler<RemovePlaylistArgs> RemovedPlaylist;
        public event EventHandler<PlaylistChangeArgs<FileMediaSources>> FileMedisSourcesChanged;
        public event EventHandler<PlaylistChangeArgs<DateTime?>> FilesLastUpdatedChanged;
        public event EventHandler<PlaylistChangeArgs<DateTime?>> SongsLastUpdatedChanged;

        public SqlitePlaylistsRepo(ISqlExecuteService sqlExecuteService) : base(sqlExecuteService)
        {
        }

        public override async Task Start()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS file_media_source_roots
                (
                    id          TEXT PRIMARY KEY,
                    update_type INTEGER NOT NULL,
                    name        TEXT    NOT NULL,
                    path_type   INTEGER NOT NULL,
                    path        TEXT    NOT NULL,
                    created     TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS playlists
                (
                    id                            TEXT PRIMARY KEY,
                    index_value                   INTEGER NOT NULL,
                    type                          INTEGER NOT NULL,
                    name                          TEXT    NOT NULL,
                    shuffle                       INTEGER NOT NULL,
                    loop                          INTEGER NOT NULL,
                    playback_rate                 REAL    NOT NULL,
                    current_song_request_id       TEXT REFERENCES songs (id),
                    current_song_request_position INTEGER,
                    current_song_request_duration INTEGER,
                    current_song_request_continue INTEGER,
                    songs_count                   INT     NOT NULL DEFAULT 0,
                    file_media_source_root_id     TEXT REFERENCES file_media_source_roots (id),
                    files_last_updated            INTEGER,
                    songs_last_updated            INTEGER,
                    created                       TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS next_playlists
                (
                    id               INTEGER PRIMARY KEY AUTOINCREMENT,
                    playlist_id      TEXT NOT NULL UNIQUE REFERENCES playlists (id),
                    next_playlist_id TEXT NOT NULL REFERENCES playlists (id),
                    created          TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS file_media_sources
                (
                    id            INTEGER PRIMARY KEY AUTOINCREMENT,
                    playlist_id   TEXT NOT NULL REFERENCES playlists (id),
                    relative_path TEXT NOT NULL,
                    created       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS songs
                (
                    id        TEXT PRIMARY KEY,
                    title     TEXT NOT NULL,
                    artist    TEXT,
                    full_path TEXT NOT NULL,
                    created   TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS playlist_songs
                (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    playlist_id TEXT    NOT NULL REFERENCES playlists (id),
                    song_id     TEXT    NOT NULL REFERENCES songs (id),
                    index_value INTEGER NOT NULL,
                    created     TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ";
            await sqlExecuteService.ExecuteNonQueryAsync(sql);
        }

        public async Task<Playlist> GetPlaylist(Guid playlistId)
        {
            string playlistSql = $@"
                SELECT p.id, type, name, shuffle, loop, playback_rate,
                    current_song_request_id, current_song_request_position, current_song_request_duration,
                    current_song_request_continue, file_media_source_root_id, np.next_playlist_id, p.files_last_updated,
                    p.songs_last_updated
                FROM playlists p
                    LEFT JOIN next_playlists np ON p.id = np.playlist_id
                WHERE p.id = @id;
            ";
            var playlistParameters = CreateParams("id", playlistId.ToString());
            (Guid id, PlaylistType type, string name, OrderType shuffle, LoopType loop, double playbackRate,
                SongRequest? currentSongRequest, Guid? fileMediaSourceRootId, Guid? nextPlaylistId,
                DateTime? filesLastUpdated, DateTime? songsLastUpdated) playlist = await sqlExecuteService.ExecuteReadFirstAsync(reader =>
            {
                Guid id = reader.GetGuidFromString("id");
                PlaylistType type = (PlaylistType)reader.GetInt64("type");
                string name = reader.GetString("name");
                OrderType shuffle = (OrderType)reader.GetInt64("shuffle");
                LoopType loop = (LoopType)reader.GetInt64("loop");
                double playbackRate = reader.GetDouble("playback_rate");
                SongRequest? currentSongRequest = GetSongRequest(reader);
                Guid? fileMediaSourceRootId = reader.GetGuidNullableFromString("file_media_source_root_id");
                Guid? nextPlaylistId = reader.GetGuidNullableFromString("next_playlist_id");
                DateTime? filesLastUpdated = reader.GetDateTimeNullableFromInt64("files_last_updated");
                DateTime? songsLastUpdated = reader.GetDateTimeNullableFromInt64("songs_last_updated");

                return (id, type, name, shuffle, loop, playbackRate, currentSongRequest, 
                    fileMediaSourceRootId, nextPlaylistId, filesLastUpdated, songsLastUpdated);
            }, playlistSql, playlistParameters);

            const string songsSql = @"
                SELECT s.id, ps.index_value, s.title, s.artist, s.full_path
                FROM songs s
                    JOIN playlist_songs ps ON ps.song_id = s.id
                WHERE ps.playlist_id = @playlistId
                ORDER BY ps.index_value;
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
                playlist.playbackRate, playlist.currentSongRequest, songs, fileMediaSources, playlist.nextPlaylistId,
                playlist.filesLastUpdated, playlist.songsLastUpdated);

            SongRequest? GetSongRequest(DbDataReader reader)
            {
                Guid? songId = reader.GetGuidNullableFromString("current_song_request_id");
                if (!songId.HasValue) return null;

                TimeSpan requestPosition = reader.GetTimespanFromInt64("current_song_request_position");
                TimeSpan requestDuration = reader.GetTimespanFromInt64("current_song_request_duration");
                bool requestContinue = reader.GetBooleanFromLong("current_song_request_continue");

                return SongRequest.Get(songId, requestPosition, requestDuration, requestContinue);
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

        private async Task UpsertFileMediaSourceRoot(FileMediaSourceRoot root)
        {
            const string fileMediaSourceRootSql = @"
                INSERT OR REPLACE INTO file_media_source_roots (id, update_type, name, path_type, path)
                VALUES (@id, @updateType, @name, @pathType, @path);
            ";
            var fileMediaSourceRootParameters = new KeyValuePair<string, object>[]
            {
                    CreateParam("id", root.Id.ToString()),
                    CreateParam("updateType", (long)root.UpdateType),
                    CreateParam("name", root.Name),
                    CreateParam("pathType", (long)root.PathType),
                    CreateParam("path", root.Path),
            };
            await sqlExecuteService.ExecuteNonQueryAsync(fileMediaSourceRootSql, fileMediaSourceRootParameters);
        }

        private async Task UpsertSong(Song song)
        {
            const string sql = @"
                INSERT OR REPLACE INTO songs (id, title, artist, full_path)
                VALUES (@id, @title, @artist, @fullPath);
            ";
            var parameters = new KeyValuePair<string, object>[]
            {
                CreateParam("id", song.Id.ToString()),
                CreateParam("title", song.Title),
                CreateParam("artist", song.Artist),
                CreateParam("fullPath", song.FullPath),
            };
            await sqlExecuteService.ExecuteNonQueryAsync(sql, parameters);
        }

        private async Task InsertIntoPlaylistTable(Playlist playlist, int? index)
        {
            if (index.HasValue)
            {
                const string moveIndexesSql = @"
                    UPDATE playlists
                    SET index_value = index_value + 1
                    WHERE index_value >= @index;
                ";
                var moveIndexesParameters = CreateParams("index", index);
                await sqlExecuteService.ExecuteNonQueryAsync(moveIndexesSql, moveIndexesParameters);
            }
            else
            {
                const string nextIndexSql = @"
                    SELECT COALESCE(MAX(index_value), -1) + 1
                    FROM playlists;
                ";
                index = (int)await sqlExecuteService.ExecuteScalarAsync<long>(nextIndexSql);
            }

            const string playlistSql = @"
                INSERT INTO playlists (id, index_value, type, name, shuffle, loop, playback_rate,
                    current_song_request_id, current_song_request_position, current_song_request_duration, current_song_request_continue,
                    songs_count, file_media_source_root_id, files_last_updated, songs_last_updated)
                VALUES (@id, @index, @type, @name, @shuffle, @loop, @playbackRate,
                    @currentSongRequestId, @currentSongRequestPosition, @currentSongRequestDuration, @currentSongRequestContinue,
                    @songsCount, @fileMediaSourceRootId, @filesLastUpdated, @songsLastUpdated);
            ";
            var playlistParameters = new KeyValuePair<string, object>[]
            {
                CreateParam("id", playlist.Id.ToString()),
                CreateParam("index", index.Value),
                CreateParam("type", (long)playlist.Type),
                CreateParam("name", playlist.Name),
                CreateParam("shuffle", (long)playlist.Shuffle),
                CreateParam("loop", (long)playlist.Loop),
                CreateParam("playbackRate", playlist.PlaybackRate),
                CreateParam("currentSongRequestId", playlist.CurrentSongRequest?.Id.ToString()),
                CreateParam("currentSongRequestPosition", playlist.CurrentSongRequest?.Position.Ticks),
                CreateParam("currentSongRequestDuration", playlist.CurrentSongRequest?.Duration.Ticks),
                CreateParam("currentSongRequestContinue", playlist.CurrentSongRequest?.ContinuePlayback == true ? 1L : 0L),
                CreateParam("fileMediaSourceRootId", playlist.FileMediaSources?.Root.Id.ToString()),
                CreateParam("songsCount", playlist.Songs?.Count ?? 0),
                CreateParam("filesLastUpdated", playlist.FilesLastUpdated?.Ticks),
                CreateParam("songsLastUpdated", playlist.SongsLastUpdated?.Ticks),
            };
            await sqlExecuteService.ExecuteNonQueryAsync(playlistSql, playlistParameters);
        }

        private async Task UpsertPlaylistSongs(Guid playlistId, ICollection<Song> songs)
        {
            int songIndex = 0;
            foreach (IList<Song> group in songs.ToGroupsOf(100))
            {
                string songsSqlValues = string.Join(",", group.Select((_, i) => $"(@id{i},@t{i},@a{i},@p{i})"));
                string songsSql = $@"
                    INSERT OR REPLACE INTO songs (id, title, artist, full_path)
                    VALUES {songsSqlValues};
                ";
                var songsParameters = group.SelectMany((song, i) => new KeyValuePair<string, object>[]
                {
                        CreateParam($"id{i}", song.Id.ToString()),
                        CreateParam($"t{i}", song.Title),
                        CreateParam($"a{i}", song.Artist),
                        CreateParam($"p{i}", song.FullPath),
                });
                await sqlExecuteService.ExecuteNonQueryAsync(songsSql, songsParameters);

                string playlistSongsSqlValues = string.Join(",", group.Select((_, i) => $"(@pid,@x{i},@sid{i})"));
                string playlistSongsSql = $@"
                    INSERT INTO playlist_songs (playlist_id, index_value, song_id)
                    VALUES {playlistSongsSqlValues};
                ";
                var playlistSongsParameters = CreateParams("pid", playlistId.ToString())
                    .Concat(group.SelectMany((song, i) => CreateParams($"x{i}", songIndex++, $"sid{i}", song.Id.ToString())))
                    .ToArray();
                await sqlExecuteService.ExecuteNonQueryAsync(playlistSongsSql, playlistSongsParameters);
            }
        }

        private async Task InsertFileMediaSources(Guid playlistId, ICollection<FileMediaSource> sources)
        {
            foreach (IList<FileMediaSource> group in sources.ToGroupsOf(100))
            {
                string fileMediaSourcesSqlValues = string.Join(",", group.Select((_, i) => $"(@pid,@rel{i})"));
                string fileMediaSourcesSql = $@"
                    INSERT INTO file_media_sources (playlist_id, relative_path)
                    VALUES {fileMediaSourcesSqlValues};
                ";
                var fileMediaSourcesParameters = CreateParams("pid", playlistId.ToString())
                    .Concat(group.Select((source, i) => CreateParam($"rel{i}", source.RelativePath)));
                await sqlExecuteService.ExecuteNonQueryAsync(fileMediaSourcesSql, fileMediaSourcesParameters);
            }
        }

        public async Task InsertPlaylist(Playlist playlist, int? index)
        {
            if (playlist.FileMediaSources != null) await UpsertFileMediaSourceRoot(playlist.FileMediaSources.Root);

            await InsertIntoPlaylistTable(playlist, index);

            if (playlist.FileMediaSources?.Sources.Count > 0) await InsertFileMediaSources(playlist.Id, playlist.FileMediaSources.Sources);
            if (playlist.Songs.Count > 0) await UpsertPlaylistSongs(playlist.Id, playlist.Songs);

            InsertedPlaylist?.Invoke(this, new InsertPlaylistArgs(index, playlist));
        }

        private async Task DeletePlaylistSongs(Guid playlistId)
        {
            const string playlistSongsSql = @"
                DELETE FROM playlist_songs
                WHERE playlist_id = @id;
            ";
            var playlistSongsParameters = CreateParams("id", playlistId.ToString());
            await sqlExecuteService.ExecuteNonQueryAsync(playlistSongsSql, playlistSongsParameters);
        }

        private async Task DeleteUnuusedSongs()
        {
            const string songsSql = @"
                DELETE FROM songs
                WHERE id NOT IN (SELECT ps.song_id FROM playlist_songs ps)
                    AND id NOT IN (SELECT p.current_song_request_id FROM playlists p);
            ";
            await sqlExecuteService.ExecuteNonQueryAsync(songsSql);
        }

        private async Task DeleteFileMediaSources(Guid playlistId)
        {
            const string fileMediaSourcesSql = @"
                DELETE FROM file_media_sources
                WHERE playlist_id = @id;
            ";
            var fileMediaSourcesParameters = CreateParams("id", playlistId.ToString());
            await sqlExecuteService.ExecuteNonQueryAsync(fileMediaSourcesSql, fileMediaSourcesParameters);
        }

        private async Task DeletePlaylist(Guid playlistId)
        {
            const string moveIndexesSql = @"
                UPDATE playlists
                SET index_value = index_value - 1
                WHERE index_value > (SELECT sub.index_value FROM playlists AS sub WHERE sub.id = @id);
            ";
            var moveIndexesParameters = CreateParams("id", playlistId.ToString());
            await sqlExecuteService.ExecuteNonQueryAsync(moveIndexesSql, moveIndexesParameters);

            const string playlistSql = @"
                DELETE FROM playlists
                WHERE id = @id;
            ";
            var playlistParameters = CreateParams("id", playlistId.ToString());
            await sqlExecuteService.ExecuteNonQueryAsync(playlistSql, playlistParameters);
        }

        private async Task DeleteUnuusedFileMediaSourceRoots()
        {
            const string fileMediaSourceRootSql = @"
                DELETE FROM file_media_source_roots
                WHERE id NOT IN (SELECT p.file_media_source_root_id FROM playlists p);
            ";
            await sqlExecuteService.ExecuteNonQueryAsync(fileMediaSourceRootSql);
        }

        public async Task RemovePlaylist(Guid playlistId)
        {
            await DeletePlaylistSongs(playlistId);
            await DeleteUnuusedSongs();
            await DeleteFileMediaSources(playlistId);
            await DeletePlaylist(playlistId);
            await DeleteUnuusedFileMediaSourceRoots();

            RemovedPlaylist?.Invoke(this, new RemovePlaylistArgs(playlistId));
        }

        private Task UpdatePlaylistValue(string columnName, Guid playlistId, object value)
        {
            string sql = $@"
                UPDATE playlists
                SET {columnName} = @value
                WHERE id = @id;
            ";
            KeyValuePair<string, object>[] parameters = CreateParams("id", playlistId.ToString(), "value", value);
            return sqlExecuteService.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task SetName(Guid playlistId, string name)
        {
            await UpdatePlaylistValue("name", playlistId, name);
            NameChanged?.Invoke(this, new PlaylistChangeArgs<string>(playlistId, name));
        }

        public async Task SetShuffle(Guid playlistId, OrderType shuffle)
        {
            await UpdatePlaylistValue("shuffle", playlistId, (long)shuffle);
            ShuffleChanged?.Invoke(this, new PlaylistChangeArgs<OrderType>(playlistId, shuffle));
        }

        public async Task SetLoop(Guid playlistId, LoopType loop)
        {
            await UpdatePlaylistValue("loop", playlistId, (long)loop);
            LoopChanged?.Invoke(this, new PlaylistChangeArgs<LoopType>(playlistId, loop));
        }

        public async Task SetPlaybackRate(Guid playlistId, double playbackRate)
        {
            await UpdatePlaylistValue("playback_rate", playlistId, playbackRate);
            PlaybackRateChanged?.Invoke(this, new PlaylistChangeArgs<double>(playlistId, playbackRate));
        }

        public async Task SetCurrentSongRequest(Guid playlistId, SongRequest? currentSongRequest)
        {
            string sql = $@"
                UPDATE playlists
                SET current_song_request_id = @currentSongRequestId,
                    current_song_request_position = @currentSongRequestPosition,
                    current_song_request_duration = @currentSongRequestDuration,
                    current_song_request_continue = @currentSongRequestContinue
                WHERE id = @id;
            ";
            KeyValuePair<string, object>[] parameters = new KeyValuePair<string, object>[]
            {
                CreateParam("id", playlistId.ToString()),
                CreateParam("currentSongRequestId", currentSongRequest?.Id.ToString()),
                CreateParam("currentSongRequestPosition", currentSongRequest?.Position.Ticks),
                CreateParam("currentSongRequestDuration", currentSongRequest?.Duration.Ticks),
                CreateParam("currentSongRequestContinue", currentSongRequest?.ContinuePlayback == true ? 1L : 0L),
            };
            await sqlExecuteService.ExecuteNonQueryAsync(sql, parameters);

            CurrentSongRequestChanged?.Invoke(this, new PlaylistChangeArgs<SongRequest?>(playlistId, currentSongRequest));
        }

        public async Task SetSongs(Guid playlistId, ICollection<Song> songs)
        {
            await DeletePlaylistSongs(playlistId);
            if (songs?.Count > 0) await UpsertPlaylistSongs(playlistId, songs);
            await UpdatePlaylistValue("songs_count", playlistId, (long)(songs?.Count ?? 0));
            await DeleteUnuusedSongs();

            SongsChanged?.Invoke(this, new PlaylistChangeArgs<ICollection<Song>>(playlistId, songs));
        }

        public async Task<ICollection<FileMediaSourceRoot>> GetFileMediaSourceRoots()
        {
            const string sql = @"
                SELECT id, update_type, name, path_type, path
                FROM file_media_source_roots;
            ";

            return await sqlExecuteService.ExecuteReadAllAsync(GetFileMediaSourceRoot, sql);

            FileMediaSourceRoot GetFileMediaSourceRoot(DbDataReader reader)
            {
                Guid id = reader.GetGuidFromString("id");
                FileMediaSourceRootUpdateType updateType = (FileMediaSourceRootUpdateType)reader.GetInt64("update_type");
                string name = reader.GetString("name");
                FileMediaSourceRootPathType pathType = (FileMediaSourceRootPathType)reader.GetInt64("path_type");
                string path = reader.GetString("path");

                return new FileMediaSourceRoot(id, updateType, name, pathType, path);
            }
        }

        public async Task<ICollection<FileMediaSource>> GetFileMediaSourcesOfRoot(Guid rootId)
        {
            const string sql = @"
                SELECT relative_path
                FROM file_media_sources fms
                    JOIN playlists p on p.id = fms.playlist_id
                WHERE p.file_media_source_root_id = @rootId;
            ";
            KeyValuePair<string, object>[] parameters = CreateParams("rootId", rootId.ToString());

            return await sqlExecuteService.ExecuteReadAllAsync(GetFileMediaSource, sql, parameters);

            FileMediaSource GetFileMediaSource(DbDataReader reader)
            {
                string relativePath = reader.GetString("relative_path");
                return new FileMediaSource(relativePath);
            }
        }

        public async Task SetFileMedisSources(Guid playlistId, FileMediaSources fileMediaSources)
        {
            await DeleteFileMediaSources(playlistId);
            if (fileMediaSources != null)
            {
                await UpsertFileMediaSourceRoot(fileMediaSources.Root);
                await InsertFileMediaSources(playlistId, fileMediaSources.Sources);
            }

            await UpdatePlaylistValue("file_media_source_root_id", playlistId, fileMediaSources?.Root.Id.ToString());
            await DeleteUnuusedFileMediaSourceRoots();

            FileMedisSourcesChanged?.Invoke(this, new PlaylistChangeArgs<FileMediaSources>(playlistId, fileMediaSources));
        }

        public async Task SetFilesLastUpdated(Guid playlistId, DateTime? filesLastUpdated)
        {
            await UpdatePlaylistValue("files_last_updated", playlistId, filesLastUpdated?.Ticks);
            FilesLastUpdatedChanged?.Invoke(this, new PlaylistChangeArgs<DateTime?>(playlistId, filesLastUpdated));
        }

        public async Task SetSongsLastUpdated(Guid playlistId, DateTime? songsLastUpdated)
        {
            await UpdatePlaylistValue("songs_last_updated", playlistId, songsLastUpdated?.Ticks);
            SongsLastUpdatedChanged?.Invoke(this, new PlaylistChangeArgs<DateTime?>(playlistId, songsLastUpdated));
        }
    }
}
