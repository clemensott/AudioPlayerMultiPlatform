using AudioPlayerBackend.Audio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public interface IPlaylistsRepo
    {
        Task<Playlist> GetPlaylist(Guid id);

        Task SendNameChange(Guid id, string name);
        event EventHandler<PlaylistChange<string>> OnNameChange;

        Task SendShuffleChange(Guid id, OrderType shuffle);
        event EventHandler<PlaylistChange<OrderType>> OnShuffleChange;

        Task SendLoopChange(Guid id, LoopType loop);
        event EventHandler<PlaylistChange<LoopType>> OnLoopChange;

        Task SendPlaybackRateChange(Guid id, double playbackRate);
        event EventHandler<PlaylistChange<double>> OnPlaybackRateChange;

        Task SendPositionChange(Guid id, TimeSpan position);
        event EventHandler<PlaylistChange<TimeSpan>> OnPositionChange;

        Task SendDurationChange(Guid id, TimeSpan duration);
        event EventHandler<PlaylistChange<TimeSpan>> OnDurationChange;

        Task SendRequestSongChange(Guid id, RequestSong? requestSong);
        event EventHandler<PlaylistChange<RequestSong?>> OnRequestSongChange;

        Task SendCurrentSongIdChange(Guid id, Guid? currentSongId);
        event EventHandler<PlaylistChange<Guid?>> OnCurrentSongIdChange;

        Task SendSongsChange(Guid id, IList<Song> songs);
        event EventHandler<PlaylistChange<IList<Song>>> OnSongsChange;
    }
}
