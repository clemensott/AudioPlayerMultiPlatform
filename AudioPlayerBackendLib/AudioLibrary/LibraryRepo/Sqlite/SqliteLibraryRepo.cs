using AudioPlayerBackend.AudioLibrary.Database.Sql;
using AudioPlayerBackend.Extensions;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo.Sqlite
{
    internal class SqliteLibraryRepo : BaseSqlRepo, ILibraryRepo
    {
        public event EventHandler<AudioLibraryChangeArgs<PlaybackState>> OnPlayStateChange;
        public event EventHandler<AudioLibraryChangeArgs<double>> OnVolumeChange;
        public event EventHandler<AudioLibraryChangeArgs<Guid?>> OnCurrentPlaylistIdChange;

        public SqliteLibraryRepo(ISqlExecuteService sqlExecuteService) : base(sqlExecuteService)
        {
        }

        public override async Task Start()
        {
            // IMPRTANT: this sql must run after the init sql in playlists repo
            //           because this sql builds on the other
            const string sql = @"
                CREATE TABLE IF NOT EXISTS libraries
                (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    play_state          INTEGER NOT NULL,
                    volume              REAL    NOT NULL,
                    current_playlist_id TEXT REFERENCES playlists (id),
                    created             TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                INSERT INTO libraries (play_state, volume, current_playlist_id)
                SELECT 2, 1, null
                FROM (SELECT 1) as d
                WHERE NOT EXISTS(SELECT id FROM libraries);
            ";
            await sqlExecuteService.ExecuteNonQueryAsync(sql);
        }

        public async Task<Library> GetLibrary()
        {
            const string librarySql = @"
                SELECT play_state, volume, current_playlist_id
                FROM libraries
                LIMIT 1;
            ";

            (PlaybackState playState, double volume, Guid? currentPlaylistId) lib = await sqlExecuteService.ExecuteReadFirstAsync(reader =>
            {
                PlaybackState playState = (PlaybackState)reader.GetInt64("play_state");
                double volume = reader.GetDouble("volume");
                string currentPlaylistIdText = reader.GetStringNullable("current_playlist_id");
                Guid? currentPlaylistId = reader.GetGuidNullableFromString("current_playlist_id");

                return (playState, volume, currentPlaylistId);
            }, librarySql);

            const string playlistsSql = @"
                SELECT id, type, name, songs_count
                FROM playlists
                ORDER BY index_value;
            ";

            IList<PlaylistInfo> playlists = await sqlExecuteService.ExecuteReadAllAsync(reader =>
            {
                Guid id = reader.GetGuidFromString("id");
                PlaylistType type = (PlaylistType)reader.GetInt64("type");
                string name = reader.GetString("name");
                int songsCount = (int)reader.GetInt64("songs_count");

                return new PlaylistInfo(id, type, name, songsCount);
            }, playlistsSql);

            return new Library(lib.playState, lib.volume, lib.currentPlaylistId, playlists);
        }

        private Task UpdateLibraryValue(string columnName, object value)
        {
            string sql = $@"
                UPDATE libraries
                SET {columnName} = @value
                WHERE 1;
            ";
            KeyValuePair<string, object>[] parameters = CreateParams("value", value);

            return sqlExecuteService.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task SendPlayStateChange(PlaybackState playState)
        {
            await UpdateLibraryValue("play_state", (long)playState);
            OnPlayStateChange?.Invoke(this, new AudioLibraryChangeArgs<PlaybackState>(playState));
        }

        public async Task SendVolumeChange(double volume)
        {
            await UpdateLibraryValue("volume", volume);
            OnVolumeChange?.Invoke(this, new AudioLibraryChangeArgs<double>(volume));
        }

        public async Task SendCurrentPlaylistIdChange(Guid? currentPlaylistId)
        {
            await UpdateLibraryValue("current_playlist_id", currentPlaylistId?.ToString());
            OnCurrentPlaylistIdChange?.Invoke(this, new AudioLibraryChangeArgs<Guid?>(currentPlaylistId));

        }
    }
}
