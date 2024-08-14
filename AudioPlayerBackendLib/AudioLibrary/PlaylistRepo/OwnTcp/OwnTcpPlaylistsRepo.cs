using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp
{
    internal class OwnTcpPlaylistsRepo : IPlaylistsRepo
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

        public Task<Playlist> GetPlaylist(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task SendInsertPlaylist(Playlist playlist, int? index)
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

        public Task SendRemovePlaylist(Guid id)
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

        public Task Start()
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }

        public Task Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
