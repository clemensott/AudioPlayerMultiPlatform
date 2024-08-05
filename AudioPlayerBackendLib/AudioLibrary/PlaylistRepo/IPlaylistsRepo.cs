using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public interface IPlaylistsRepo : IAudioService
    {
        Task<Playlist> GetPlaylist(Guid id);

        Task SendInsertPlaylist(Playlist playlist, int index);
        event EventHandler<InsertPlaylistArgs> OnInsertPlaylist;

        Task SendRemovePlaylist(Guid id);
        event EventHandler<RemovePlaylistArgs> OnRemovePlaylist;

        Task SendNameChange(Guid id, string name);
        event EventHandler<PlaylistChangeArgs<string>> OnNameChange;

        Task SendShuffleChange(Guid id, OrderType shuffle);
        event EventHandler<PlaylistChangeArgs<OrderType>> OnShuffleChange;

        Task SendLoopChange(Guid id, LoopType loop);
        event EventHandler<PlaylistChangeArgs<LoopType>> OnLoopChange;

        Task SendPlaybackRateChange(Guid id, double playbackRate);
        event EventHandler<PlaylistChangeArgs<double>> OnPlaybackRateChange;

        Task SendPositionChange(Guid id, TimeSpan position);
        event EventHandler<PlaylistChangeArgs<TimeSpan>> OnPositionChange;

        Task SendDurationChange(Guid id, TimeSpan duration);
        event EventHandler<PlaylistChangeArgs<TimeSpan>> OnDurationChange;

        Task SendRequestSongChange(Guid id, RequestSong? requestSong);
        event EventHandler<PlaylistChangeArgs<RequestSong?>> OnRequestSongChange;

        Task SendCurrentSongIdChange(Guid id, Guid? currentSongId);
        event EventHandler<PlaylistChangeArgs<Guid?>> OnCurrentSongIdChange;

        Task SendSongsChange(Guid id, ICollection<Song> songs);
        event EventHandler<PlaylistChangeArgs<ICollection<Song>>> OnSongsChange;

        Task SendFileMedisSourcesChange(Guid id, FileMediaSources fileMediaSources);
        event EventHandler<PlaylistChangeArgs<FileMediaSources>> OnFileMedisSourcesChange;
    }
}
