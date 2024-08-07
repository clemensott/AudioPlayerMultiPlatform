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

        public async Task<Library> GetLibrary()
        {
            const string librarySql = @"
                SELECT play_state, volume, current_playlist_id
                FROM libaries
                LIMIT 1;
            ";

            var lib = await sqlExecuteService.ExecuteReadFirstAsync(reader =>
            {
                PlaybackState playState = (PlaybackState)reader.GetInt64("play_state");
                double volume = reader.GetDouble("volume");
                string currentPlaylistIdText = reader.GetStringNullable("current_playlist_id");
                Guid? currentPlaylistId = reader.GetGuidNullableFromString("current_playlist_id");

                return (playState, volume, currentPlaylistId);
            }, librarySql);

            const string playlistsSql = @"
                SELECT id, type, name, songs_count
                FROM playlists;
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
                UPDATE libaries
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

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public Task Dispose()
        {
            sqlExecuteService.Dispose();
            return Task.CompletedTask;
        }
    }
}
