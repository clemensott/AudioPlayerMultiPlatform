using AudioPlayerBackend.Audio;
using AudioPlayerBackend.AudioLibrary;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.Sqlite
{
    internal class SqlitePlaylistsRepo : IPlaylistsRepo
    {
        public event EventHandler<PlaylistChange<string>> OnNameChange;
        public event EventHandler<PlaylistChange<OrderType>> OnShuffleChange;
        public event EventHandler<PlaylistChange<LoopType>> OnLoopChange;
        public event EventHandler<PlaylistChange<double>> OnPlaybackRateChange;
        public event EventHandler<PlaylistChange<TimeSpan>> OnPositionChange;
        public event EventHandler<PlaylistChange<TimeSpan>> OnDurationChange;
        public event EventHandler<PlaylistChange<RequestSong?>> OnRequestSongChange;
        public event EventHandler<PlaylistChange<Guid?>> OnCurrentSongIdChange;
        public event EventHandler<PlaylistChange<IList<Song>>> OnSongsChange;

        public Task<Playlist> GetPlaylist(Guid id)
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

        public Task SendRequestSongChange(Guid id, RequestSong? requestSong)
        {
            throw new NotImplementedException();
        }

        public Task SendShuffleChange(Guid id, OrderType shuffle)
        {
            throw new NotImplementedException();
        }

        public Task SendSongsChange(Guid id, IList<Song> songs)
        {
            throw new NotImplementedException();
        }
    }
}
