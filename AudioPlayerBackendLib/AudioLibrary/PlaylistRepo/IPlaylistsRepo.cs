using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AudioPlayerBackend.AudioLibrary.PlaylistRepo
{
    public interface IPlaylistsRepo : IAudioService
    {
        Task<Playlist> GetPlaylist(Guid id);

        Task InsertPlaylist(Playlist playlist, int? index);
        event EventHandler<InsertPlaylistArgs> InsertedPlaylist;

        Task RemovePlaylist(Guid id);
        event EventHandler<RemovePlaylistArgs> RemovedPlaylist;

        Task SetName(Guid id, string name);
        event EventHandler<PlaylistChangeArgs<string>> NameChanged;

        Task SetShuffle(Guid id, OrderType shuffle);
        event EventHandler<PlaylistChangeArgs<OrderType>> ShuffleChanged;

        Task SetLoop(Guid id, LoopType loop);
        event EventHandler<PlaylistChangeArgs<LoopType>> LoopChanged;

        Task SetPlaybackRate(Guid id, double playbackRate);
        event EventHandler<PlaylistChangeArgs<double>> PlaybackRateChanged;

        Task SetCurrentSongRequest(Guid id, SongRequest? currentSongRequest);
        event EventHandler<PlaylistChangeArgs<SongRequest?>> CurrentSongRequestChanged;

        Task SetSongs(Guid id, ICollection<Song> songs);
        event EventHandler<PlaylistChangeArgs<ICollection<Song>>> SongsChanged;

        Task<ICollection<FileMediaSourceRoot>> GetFileMediaSourceRoots();
        
        Task<ICollection<FileMediaSource>> GetFileMediaSourcesOfRoot(Guid rootId);

        Task SetFileMedisSources(Guid id, FileMediaSources fileMediaSources);
        event EventHandler<PlaylistChangeArgs<FileMediaSources>> FileMedisSourcesChanged;

        Task SetFilesLastUpdated(Guid id, DateTime? filesLastUpdated);
        event EventHandler<PlaylistChangeArgs<DateTime?>> FilesLastUpdatedChanged;

        Task SetSongsLastUpdated(Guid id, DateTime? songsLastUpdated);
        event EventHandler<PlaylistChangeArgs<DateTime?>> SongsLastUpdatedChanged;
    }
}
