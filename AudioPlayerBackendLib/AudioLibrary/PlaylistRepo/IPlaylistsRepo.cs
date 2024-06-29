using AudioPlayerBackend.Audio.MediaSource;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public interface IPlaylistsRepo
    {
        Task<Playlist> GetPlaylist(Guid id);

        Task<FileMediaSource> GetAllFileMediaSources();

        Task InsertPlaylist(Playlist playlist, int index);
        event EventHandler<InsertPlaylistArgs> OnInsertPlaylist;

        Task RemovePlaylist(Guid id);
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

        Task SendSongsChange(Guid id, IList<Song> songs);
        event EventHandler<PlaylistChangeArgs<IList<Song>>> OnSongsChange;

        Task SendFileMedisSourcesChange(Guid id, IList<FileMediaSource> fileMediaSources);
        event EventHandler<PlaylistChangeArgs<IList<FileMediaSource>>> OnFileMedisSourcesChange;
    }
}
